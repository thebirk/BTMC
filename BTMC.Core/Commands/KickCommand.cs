using GbxRemoteNet;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTMC.Core.Commands
{
    [Command("kick")]
    class KickCommand : CommandBase
    {
        private readonly ILogger<KickCommand> _logger;

        public KickCommand(ILogger<KickCommand> logger)
        {
            _logger = logger;
        }

        public override async Task ExecuteAsync()
        {
            if (Args.Length != 1)
            {
                await SendMessageAsync("Kick - Usage: /kick <login/nick>");
                return;
            }

            string kickLogin = Args[0];

            var players = await Client.GetPlayerListAsync();
            if (!players.Any(x => x.NickName == kickLogin || x.Login == kickLogin))
            {
                await SendMessageAsync($"Kick - Unknown login/nick '{Args[0]}'");
                return;
            }

            var playerInfo = await Client.GetPlayerInfoAsync(Args[0]);
            _logger.LogDebug("hullo " + playerInfo);

            if (!await Client.KickAsync(Args[0]))
            {
                await SendMessageAsync($"Kick - Could not kick '{Args[0]}'");
                return;
            }

            _logger.LogInformation("Kicked " + Args[0]);

            return;
        }
    }
}
