using System.Collections.Generic;
using System.Threading.Tasks;
using GbxRemoteNet.XmlRpc;

namespace BTMC.Core
{
    public class PlayerInfoCache
    {
        public string Login { get; set; }
        public string Nickname { get; set; }
        public int SpectatorStatus { get; set; }
        public int Flags { get; set; }
    }
    
    [Plugin("PlayerController", "0.0.1")]
    public class PlayerController
    {
        private readonly Dictionary<string, PlayerInfoCache> _playerInfoCache = new();

        public PlayerController()
        {
        }

        [EventHandler(EventType.Load)]
        public async Task<bool> OnLoad(LoadEvent e)
        {
            return false;
        }

        [EventHandler(EventType.Join)]
        public async Task<bool> OnJoin(PlayerJoinEvent e)
        {
            var a = await e.Client.CallOrFaultAsync("GetPlayerInfo", e.Login, 0);
            var playerInfo = (PlayerInfo)XmlRpcTypes.ToNativeValue<PlayerInfo>(a);

            _playerInfoCache[e.Login] = new PlayerInfoCache
            {
                Login = playerInfo.Login,
                Nickname = playerInfo.NickName,
            };

            return false;
        }

        [EventHandler(EventType.PlayerInfo)]
        public Task<bool> OnPlayerInfo(PlayerInfoEvent e)
        {
            if (_playerInfoCache.ContainsKey(e.Login))
            {
                var cache = _playerInfoCache[e.Login];
                cache.Login = e.Login;
                cache.Nickname = e.Nickname;
                cache.Flags = e.Flags;
                cache.SpectatorStatus = e.SpectatorStatus;
            }
            else
            {
                _playerInfoCache[e.Login] = new PlayerInfoCache
                {
                    Login = e.Login,
                    Nickname = e.Nickname,
                    Flags = e.Flags,
                    SpectatorStatus = e.SpectatorStatus,
                };
            }
            return Task.FromResult(false);
        }
    }
}