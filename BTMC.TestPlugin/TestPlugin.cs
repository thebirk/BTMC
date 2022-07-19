using System;
using System.Threading.Tasks;
using BTMC.Core;
using GbxRemoteNet.XmlRpc;
using Microsoft.Extensions.Logging;

namespace BTMC.TestPlugin
{
    public class TestPlugin : IPlugin
    {
        public string Name => "Test Plugin";
        public string Version => "0.0.1";

        private readonly ILogger<TestPlugin> _logger;

        public TestPlugin(ILogger<TestPlugin> logger)
        {
            _logger = logger;
        }
        
        public void Unload()
        {
            
        }

        [EventHandler(EventType.Join)]
        public async Task<bool> OnJoin(PlayerJoinEvent e)
        {
            _logger.LogInformation("login: {}, isSpectator: {}", e.Login, e.IsSpectator);
            var a = await e.Client.CallOrFaultAsync("GetPlayerInfo", e.Login, 0);
            var playerInfo = (PlayerInfo)XmlRpcTypes.ToNativeValue<PlayerInfo>(a);
            await e.Client.ChatSendServerMessageAsync($"{playerInfo.NickName} has {(e.IsSpectator ? "joined as a spectator" : "joined the server")}");
            
            return false;
        }
    }
}