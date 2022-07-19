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

    public class CommandRepository
    {
        public Dictionary<string, Type> AllCommands { get; private set; } = new Dictionary<string, Type>();
        public Dictionary<string, Type> Commands { get; private set; } = new Dictionary<string, Type>();
    }

    public interface IPlugin
    {
        public string Name { get; }
        public string Version { get; }

        public void Unload();
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class EventHandlerAttribute : Attribute
    {
        public EventType Type { get; set; }

        public EventHandlerAttribute(EventType type)
        {
            Type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SettingsAttribute : Attribute
    {
    }

    public class PlayerInfo
    {
        public string Login;
        public string NickName;
        public int PlayerId;
        public int TeamId;
        public bool IsSpectator;
        public bool IsInOfficialMode;
        public int LadderRanking;
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

            _client.OnPlayerConnect += async (login, isSpectator) =>
            {
                _logger.LogInformation("Player Connected: " + login + ", isSpectator: " + isSpectator);
                await _eventSystem.DispatchAsync(new PlayerJoinEvent(_client, login, isSpectator));
            };

            _client.OnPlayerChat += async (int playerUid, string login, string text, bool isRegisteredCmd) =>
            {
                _logger.LogDebug("OnPlayerChat");
                // Ignore messages sent by the server
                if (playerUid == 0)
                {
                    return;
                }

                //XmlRpcTypes.ToNativeValue<>
                //var playerInfo = (PlayerInfo) XmlRpcTypes.ToNativeStruct<PlayerInfo>((XmlRpcStruct)await _client.CallOrFaultAsync("GetPlayerInfo", login, 0));
                var a = await _client.CallOrFaultAsync("GetPlayerInfo", login, 0);
                var playerInfo = (PlayerInfo)XmlRpcTypes.ToNativeValue<PlayerInfo>(a);
                _logger.LogInformation($"PlayerChat: [{playerInfo.NickName}] playerUId {playerUid} - login {login} - text {text} - isRegisteredCmd {isRegisteredCmd}");

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

                    var command = InstantiateCommandFromName(_serviceProvider, args[0]);
                    _logger.LogDebug($"Command {command} for {args[0]}");
                    command.Init(_client, playerUid, login, args[1..]);
                    await command.ExecuteAsync();
                }
                else
                {
                    await _client.ChatSendServerMessageAsync(playerInfo.NickName + "$g$z: " + text.Trim());
                }

                return;
            };

            _logger.LogInformation("Enabling callbacks..");
            await _client.EnableCallbacksAsync(true);

            _logger.LogInformation("Enabling manual routing..");
            await _client.CallOrFaultAsync("ChatEnableManualRouting", true, false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.DisconnectAsync();
        }

        private CommandBase InstantiateCommandFromName(IServiceProvider serviceProvider, string Name)
        {
            if (!_commandRepository.Commands.ContainsKey(Name))
            {
                throw new Exception($"Tried to instante Command with name {Name} but no such command exists");
            }

            var command = InstantiateCommandFromType(serviceProvider, _commandRepository.Commands[Name]);
            return command;
        }

        private CommandBase InstantiateCommandFromType(IServiceProvider serviceProvider, Type t)
        {
            var instance = ActivatorUtilities.CreateInstance(serviceProvider, t);
            return (CommandBase)instance;
        }

        private void RegisterAllCommands()
        {
            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (t.GetCustomAttributes(typeof(CommandAttribute), true).Length > 0)
                {
                    var attribute = t.GetCustomAttribute<CommandAttribute>();

                    if (_commandRepository.Commands.ContainsKey(attribute.Name))
                    {
                        throw new Exception($"Cannot add command '{attribute.Name}' as there is a already a command/alias with that name");
                    }

                    _commandRepository.Commands[attribute.Name] = t;
                    _commandRepository.AllCommands[attribute.Name] = t;

                    if (!string.IsNullOrEmpty(attribute.Alias))
                    {
                        var aliases = attribute.Alias.Split(',');
                        foreach (var alias in aliases)
                        {
                            var trimmed = alias.Trim();
                            if (_commandRepository.Commands.ContainsKey(trimmed))
                            {
                                throw new Exception($"Cannot add alias '{trimmed}' as there is a already a command/alias with that name");
                            }

                            _commandRepository.Commands[alias.Trim()] = t;
                        }
                    }
                }
            }
        }

        private void RegisterAllEventHandlers()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.DefinedTypes)
                {
                    if (t != typeof(IPlugin) && t.IsAssignableTo(typeof(IPlugin)))
                    {
                        var pluginInstance = (IPlugin) _serviceProvider.GetService(t.AsType());
                        _logger.LogInformation("Loaded plugin: {} ({})", pluginInstance.Name, pluginInstance.Version);

                        foreach (var method in t.GetMethods())
                        {
                            var attribute = method.GetCustomAttribute<EventHandlerAttribute>();
                            if (attribute == null)
                            {
                                continue;
                            }
                            
                            // TODO: typecheck method arguments
                            
                            var handler = Delegate.CreateDelegate(typeof(EventHandler<PlayerJoinEvent>), pluginInstance, method);
                            _eventSystem.RegisterEventHandler(attribute.Type, handler);
                            _logger.LogInformation("{}: Registered event handler {} for event {}", pluginInstance.Name, method.Name, attribute.Type);
                        }
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
                if (t != typeof(IPlugin) && t.IsAssignableTo(typeof(IPlugin)))
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

                    RegisterAllPlugins(services);

                    services.BuildServiceProvider();
                });
        }
    }
}