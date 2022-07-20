using BTMC.Core.Commands;
using GbxRemoteNet;
using GbxRemoteNet.XmlRpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using GbxRemoteNet.XmlRpc.Types;

namespace BTMC.Core
{
    public class GbxRemoteSettingsSuperAdmin
    {
        public string Name { get; set; }
        public string Password { get; set; }
    }

    public class GbxRemoteSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public GbxRemoteSettingsSuperAdmin SuperAdmin { get; set; }
    }

    public enum CommandDefinitionType
    {
        Command,
        BasicCommand,
    }
    
    public class CommandDefinition
    {
        public CommandDefinitionType Type { get; set; }
        public Type CommandClassType { get; set; }
        public BasicCommandHandler BasicCommandHandler { get; set; }
    }

    public class CommandRepository
    {
        /// <summary>
        /// All unique commands
        /// </summary>
        public Dictionary<string, CommandDefinition> AllCommands { get; private set; } = new Dictionary<string, CommandDefinition>();
        /// <summary>
        /// All commands with their aliases also represented
        /// </summary>
        public Dictionary<string, CommandDefinition> Commands { get; private set; } = new Dictionary<string, CommandDefinition>();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SettingsAttribute : Attribute
    {
    }

    public class GbxRemoteService : IHostedService
    {
        private readonly ILogger<GbxRemoteService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandRepository _commandRepository;
        private readonly EventSystem _eventSystem;

        private GbxRemoteClient _client { get; set; }

        public GbxRemoteService(ILogger<GbxRemoteService> logger, IConfiguration configuration, IServiceProvider serviceProvider, CommandRepository commandRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _commandRepository = commandRepository;
            _eventSystem = new EventSystem();

            RegisterAllCommands();
            RegisterAllEventHandlers();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var settings = new GbxRemoteSettings();
            _configuration.Bind("GbxRemote", settings);

            _logger.LogInformation($"Creating client for {settings.Host}:{settings.Port}");
            _client = new GbxRemoteClient(settings.Host, settings.Port);

            //await _client.LoginAsync(settings.SuperAdmin.Name, settings.SuperAdmin.Password);

            if (!await _client.ConnectAsync())
            {
                var error = $"Failed to connect to client {settings.Host}:{settings.Port}";
                _logger.LogCritical(error);
                throw new Exception(error);
            }
            _logger.LogInformation($"Connected to client {settings.Host}:{settings.Port}");
            
            //await _client.SetApiVersionAsync("2013-04-16");
            //await _client.SetApiVersionAsync("2019-03-02");
            //_logger.LogInformation("{}", (await _client.GetVersionAsync()).ApiVersion);
            
            if (!await _client.AuthenticateAsync(settings.SuperAdmin.Name, settings.SuperAdmin.Password))
            {
                var error = $"Failed to login to client {settings.Host}:{settings.Port}";
                _logger.LogCritical(error);
                throw new Exception(error);
            }
            _logger.LogInformation($"Authenticated for client {settings.Host}:{settings.Port}");

            _client.OnPlayerConnect += async (string login, bool isSpectator) =>
            {
                _logger.LogInformation("Player disconnected: {}, isSpectator: {}", login, isSpectator);
                await _eventSystem.DispatchAsync(new PlayerJoinEvent(_client, login, isSpectator));
            };

            _client.OnPlayerDisconnect += async (string login, string reason) =>
            {
                _logger.LogInformation("Player disconnected: {}, reason: {}", login, reason);
                await _eventSystem.DispatchAsync(new PlayerDisconnectEvent(_client, login, reason));
            };

            await _client.CallMethodAsync("system.listMethods");

            _client.OnPlayerChat += async (int playerUid, string login, string text, bool isRegisteredCmd) =>
            {
                // Ignore messages sent by the server
                if (playerUid == 0)
                {
                    return;
                }

                if (text.Trim().StartsWith('/'))
                {
                    isRegisteredCmd = true;
                }

                if (isRegisteredCmd)
                {
                    string[] args = CommandBase.ParseArgs(text);
                    if (!_commandRepository.Commands.ContainsKey(args[0]))
                    {
                        await _client.ChatSendServerMessageToIdAsync($"Unknown command '{args[0]}'", playerUid);
                        return;
                    }

                    var sw = new Stopwatch();
                    sw.Start();
                    await RunCommandAsync(args[0], _client, playerUid, login, args[1..]);
                    sw.Stop();
                    _logger.LogInformation("Running command '{}' took {}ms", args[0], sw.ElapsedMilliseconds);
                }
                else
                {
                    await _eventSystem.DispatchAsync(new PlayerChatEvent(_client, login, playerUid, text));
                }
            };

            _logger.LogInformation("Enabling callbacks..");
            await _client.EnableCallbacksAsync(true);

            //_logger.LogInformation("Enabling manual routing..");
            //await _client.CallOrFaultAsync("ChatEnableManualRouting", true, false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.DisconnectAsync();
        }

        private Task RunCommandAsync(string name, GbxRemoteClient client, int playerUid, string playerLogin, string[] args)
        {
            var commandDefinition = _commandRepository.Commands[name];

            switch (commandDefinition.Type)
            {
                case CommandDefinitionType.Command:
                    var command = InstantiateCommandFromName(_serviceProvider, name);
                    //_logger.LogDebug($"Command {command} for {name}");
                    command.Init(_client, playerUid, playerLogin, args);
                    return command.ExecuteAsync();
                case CommandDefinitionType.BasicCommand:
                    return commandDefinition.BasicCommandHandler.Invoke(new CommandArgs
                    {
                        Args = args,
                        Client = client,
                        PlayerUid = playerUid,
                        PlayerLogin = playerLogin
                    });
                default:
                    _logger.LogCritical("Invalid CommandDefinitionType {}", commandDefinition.Type);
                    throw new Exception($"Invalid CommandDefinitionType {commandDefinition.Type}");
            }
        }
        
        private CommandBase InstantiateCommandFromName(IServiceProvider serviceProvider, string Name)
        {
            if (!_commandRepository.Commands.ContainsKey(Name))
            {
                throw new Exception($"Tried to instante Command with name {Name} but no such command exists");
            }

            var command = InstantiateCommandFromType(serviceProvider, _commandRepository.Commands[Name].CommandClassType);
            return command;
        }

        private CommandBase InstantiateCommandFromType(IServiceProvider serviceProvider, Type t)
        {
            var instance = ActivatorUtilities.CreateInstance(serviceProvider, t);
            return (CommandBase)instance;
        }

        private void RegisterAllCommands()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
                    {
                        var attribute = t.GetCustomAttribute<CommandAttribute>();

                        if (_commandRepository.Commands.ContainsKey(attribute.Name))
                        {
                            throw new Exception($"Cannot add command '{attribute.Name}' as there is a already a command/alias with that name");
                        }

                        _commandRepository.Commands[attribute.Name] = new CommandDefinition
                        {
                            Type = CommandDefinitionType.Command,
                            CommandClassType = t,
                        };
                        _commandRepository.AllCommands[attribute.Name] = new CommandDefinition
                        {
                            Type = CommandDefinitionType.Command,
                            CommandClassType = t,
                        };

                        if (attribute.Aliases != null)
                        {
                            foreach (var alias in attribute.Aliases)
                            {
                                var trimmed = alias.Trim();
                                if (_commandRepository.Commands.ContainsKey(trimmed))
                                {
                                    throw new Exception($"Cannot add alias '{trimmed}' as there is a already a command/alias with that name");
                                }

                                _commandRepository.Commands[alias.Trim()] = new CommandDefinition
                                {
                                    Type = CommandDefinitionType.Command,
                                    CommandClassType = t,
                                };
                            }
                        }
                    }
                    else if (t.GetCustomAttributes(typeof(PluginAttribute), false).Length > 0)
                    {
                        var pluginInstance = _serviceProvider.GetService(t);
                        var pluginAttribute = t.GetCustomAttribute<PluginAttribute>(false);

                        foreach (var method in t.GetMethods())
                        {
                            var attribute = method.GetCustomAttribute<CommandAttribute>();
                            if (attribute == null)
                            {
                                continue;
                            }

                            var handler = Delegate.CreateDelegate(typeof(BasicCommandHandler), pluginInstance, method, false);
                            if (handler == null)
                            {
                                // signature does not match, we were not able to create a delegate
                                _logger.LogError(
                                    "{}: Failed to register command '{}'. Invalid method signature. Expected '{}', found '{}'",
                                    pluginAttribute.Name,
                                    attribute.Name,
                                    typeof(BasicCommandHandler).GetMethods()[0].ToString(),
                                    method.ToString()
                                );
                                continue;
                            }
                            
                            if (_commandRepository.Commands.ContainsKey(attribute.Name))
                            {
                                throw new Exception($"Cannot add command '{attribute.Name}' as there is a already a command/alias with that name");
                            }
                            
                            
                            _commandRepository.Commands[attribute.Name] = new CommandDefinition
                            {
                                Type = CommandDefinitionType.BasicCommand,
                                BasicCommandHandler = (BasicCommandHandler) handler,
                            };
                            _commandRepository.AllCommands[attribute.Name] = new CommandDefinition
                            {
                                Type = CommandDefinitionType.BasicCommand,
                                BasicCommandHandler = (BasicCommandHandler) handler,
                            };

                            if (attribute.Aliases != null)
                            {
                                foreach (var alias in attribute.Aliases)
                                {
                                    var trimmed = alias.Trim();
                                    if (_commandRepository.Commands.ContainsKey(trimmed))
                                    {
                                        throw new Exception($"Cannot add alias '{trimmed}' as there is a already a command/alias with that name");
                                    }

                                    _commandRepository.Commands[alias.Trim()] = new CommandDefinition
                                    {
                                        Type = CommandDefinitionType.BasicCommand,
                                        BasicCommandHandler = (BasicCommandHandler) handler,
                                    };
                                }
                            }
                            
                            _logger.LogInformation("{}: Registered basic command '{}'", pluginAttribute.Name, attribute.Name);
                        }
                    }
                }
            }
        }

        private Delegate CreateEventHandlerDelegate<TEvent>(PluginAttribute pluginAttribute, object pluginInstance, MethodInfo method) where TEvent : Event
        {
            var handler = Delegate.CreateDelegate(typeof(EventHandler<TEvent>), pluginInstance, method, false);
            if (handler == null)
            {
                _logger.LogError(
                    "{}: Failed to register event handler '{}'. Invalid method signature. Expected '{}', found '{}'",
                    pluginAttribute.Name,
                    method.Name,
                    typeof(EventHandler<TEvent>).GetMethods()[0].ToString(),
                    method.ToString()
                );

                return null;
            }

            return handler;
        }
        
        private void RegisterAllEventHandlers()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.DefinedTypes)
                {
                    if (t.GetCustomAttributes(typeof(PluginAttribute), false).Length > 0)
                    {
                        var pluginInstance = _serviceProvider.GetService(t.AsType());
                        var pluginAttribute = t.GetCustomAttribute<PluginAttribute>();
                        _logger.LogInformation("Loading plugin: {} ({})...", pluginAttribute.Name, pluginAttribute.Version);

                        foreach (var method in t.GetMethods())
                        {
                            var attribute = method.GetCustomAttribute<EventHandlerAttribute>();
                            if (attribute == null)
                            {
                                continue;
                            }

                            Delegate handler;
                            switch (attribute.Type)
                            {
                                case EventType.Join:
                                    handler = CreateEventHandlerDelegate<PlayerJoinEvent>(pluginAttribute, pluginInstance, method);
                                    break;
                                case EventType.Chat:
                                    handler = CreateEventHandlerDelegate<PlayerChatEvent>(pluginAttribute, pluginInstance, method);
                                    break;
                                case EventType.Disconnect:
                                    handler = CreateEventHandlerDelegate<PlayerDisconnectEvent>(pluginAttribute, pluginInstance, method);
                                    break;
                                default:
                                    throw new Exception($"Invalid EventType enum: {attribute.Type}");
                            }
                            
                            if (handler == null)
                            {
                                continue;
                            }
                            
                            _eventSystem.RegisterEventHandler(attribute.Type, handler);
                            _logger.LogInformation("{}: Registered event handler {} for event {}", pluginAttribute.Name, method.Name, attribute.Type);
                        }
                        _logger.LogInformation("Loaded plugin: {} ({})", pluginAttribute.Name, pluginAttribute.Version);
                    }
                }
            }
        }
    }

    public class Program
    {
        static void RegisterAllPlugins(IServiceCollection services)
        {
            var all = Assembly.GetExecutingAssembly().DefinedTypes.ToList();

            foreach (var path in Directory.GetFiles(Environment.CurrentDirectory))
            {
                if (path.EndsWith(".dll"))
                {
                    all.AddRange(Assembly.LoadFile(path).DefinedTypes);
                }
            }

            foreach (var t in all)
            {
                if (t.GetCustomAttributes(typeof(PluginAttribute), false).Length > 0)
                {
                    Console.WriteLine("Found plugin: {0}", t.FullName);
                    services.AddSingleton(t.AsType());
                }
            }
        }

        public static void Start(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder =>
                {

                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<CommandRepository>();
                    services.AddHostedService<GbxRemoteService>();
                    services.AddSingleton<AdminController>();

                    RegisterAllPlugins(services);

                    services.BuildServiceProvider();
                });
        }
    }
}