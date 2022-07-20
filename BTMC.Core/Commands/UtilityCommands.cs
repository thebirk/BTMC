using GbxRemoteNet;
using GbxRemoteNet.Structs;
using GbxRemoteNet.XmlRpc;
using GbxRemoteNet.XmlRpc.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTMC.Core.Commands
{
    [Command("ping")]
    class PingCommand : CommandBase
    {
        public override async Task ExecuteAsync()
        {
            await SendMessageAsync(
                Args.Length > 0
                    ? $"Pong - {string.Join(", ", Args.Select(x => $"\"{x}\""))}"
                    : "Pong"
            );
        }
    }

    [Command("testmania")]
    class TestmaniaCommand : CommandBase
    {
        public override async Task ExecuteAsync()
        {
            string xml = File.ReadAllText("manialink.xml");
            await Client.SendDisplayManialinkPageAsync(xml, 20000, true);
        }
    }

    [Command("help")]
    class HelpCommand : CommandBase
    {
        private ILogger<HelpCommand> _logger;
        private readonly CommandRepository _commandRepository;

        public HelpCommand(ILogger<HelpCommand> logger, CommandRepository commandRepository)
        {
            _logger = logger;
            _commandRepository = commandRepository;
        }

        public override async Task ExecuteAsync()
        {
            await SendMessageAsync("All commands:");

            foreach (var cmd in _commandRepository.AllCommands.Keys)
            {
                await SendMessageAsync(
                    $"- {cmd}"
                );
            }
        }
    }

    [Command("players", "list", "online")]
    class PlayersCommand : CommandBase
    {
        private readonly ILogger<PlayersCommand> _logger;

        public PlayersCommand(ILogger<PlayersCommand> logger)
        {
            _logger = logger;
        }

        public override async Task ExecuteAsync()
        {
            //var players = await Client.GetPlayerListAsync(-1, 0, 1);
            _logger.LogInformation("players1");
            var result = await Client.CallOrFaultAsync("GetPlayerList", -1, 0, 1);
            var players = XmlRpcTypes.ToNativeArray<PlayerInfo>(
                (XmlRpcArray)result
            );
            _logger.LogInformation("players2");
            foreach (var player in players)
            {
                await SendMessageAsync($"- {player.Login} - {player.NickName}");
            }
        }
    }

    [Command("status")]
    class StatusCommand : CommandBase
    {
        public override async Task ExecuteAsync()
        {
            var status = await Client.GetStatusAsync();
            await SendMessageAsync($"{status.Code} - {status.Name}");
        }
    }

    [Command("run")]
    class RunCommand : CommandBase
    {
        public override async Task ExecuteAsync()
        {
            if (Args.Length < 1)
            {
                await SendMessageAsync("Run - Usage: /run method [options..]");
                return;
            }

            List<XmlRpcBaseType> xmlArgs = new List<XmlRpcBaseType>();

            foreach (var arg in Args[1..])
            {
                if (bool.TryParse(arg, out var boolArg))
                {
                    xmlArgs.Add(XmlRpcTypes.ToXmlRpcValue(boolArg));
                }
                else if (int.TryParse(arg, out var intArg))
                {
                    xmlArgs.Add(XmlRpcTypes.ToXmlRpcValue(intArg));
                }
                else
                {
                    xmlArgs.Add(XmlRpcTypes.ToXmlRpcValue(arg));
                }
            }

            var response = await Client.CallAsync(Args[0], xmlArgs.ToArray());

            if (response.IsFault)
            {
                await SendMessageAsync("Run - Fault - " + response.RawMessage);
                return;
            }

            await SendMessageAsync("Run - " + string.Join(" ", Args));
            await SendMessageAsync(response.RawMessage);
        }
    }
}
