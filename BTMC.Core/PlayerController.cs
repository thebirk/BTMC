using System.Collections.Generic;
using System.Threading.Tasks;
using GbxRemoteNet;
using GbxRemoteNet.XmlRpc;
using GbxRemoteNet.XmlRpc.Types;

namespace BTMC.Core
{
    [Plugin("PlayerController", "0.0.1")]
    public class PlayerController
    {
        public class PlayerInfo
        {
            public string Login { get; set; }
            public string Nickname { get; set; }
            public bool IsSpectator { get; set; }
            public int PlayerUid { get; set; }
            public int TeamId { get; set; }
            // tmnf stuff?
            public int LadderRanking { get; set; }
            public bool IsInOfficialMode { get; set; }
        }
        
        private readonly GbxRemoteClient _client;
        private readonly Dictionary<string, PlayerInfo> _playerInfoCache = new();

        public PlayerController(GbxRemoteService gbxRemoteService)
        {
            _client = gbxRemoteService.Client;
        }
        
        public PlayerInfo GetPlayerInfo(string login)
        {
            if (!_playerInfoCache.TryGetValue(login, out var playerInfo))
            {
                return null;
            }

            return playerInfo;
        }

        [EventHandler(EventType.Load)]
        public async Task<bool> OnLoad(LoadEvent e)
        {
            var a = await _client.CallOrFaultAsync("GetPlayerList", 0, 0, 0);
            var playerList = XmlRpcTypes.ToNativeArray<Core.PlayerInfo>((XmlRpcArray) a);

            foreach (var playerInfo in playerList)
            {
                _playerInfoCache[playerInfo.Login] = new PlayerInfo
                {
                    Login = playerInfo.Login,
                    Nickname = playerInfo.NickName,
                    IsSpectator = playerInfo.IsSpectator,
                    PlayerUid = playerInfo.PlayerId,
                    TeamId = playerInfo.TeamId,
                    LadderRanking = playerInfo.LadderRanking,
                    IsInOfficialMode = playerInfo.IsInOfficialMode,
                };
            }

            return false;
        }

        [EventHandler(EventType.Join)]
        public async Task<bool> OnJoin(PlayerJoinEvent e)
        {
            var a = await _client.CallOrFaultAsync("GetPlayerInfo", e.Login, 0);
            var playerInfo = (Core.PlayerInfo)XmlRpcTypes.ToNativeValue<Core.PlayerInfo>(a);
            
            _playerInfoCache[e.Login] = new PlayerInfo
            {
                Login = playerInfo.Login,
                Nickname = playerInfo.NickName,
                IsSpectator = playerInfo.IsSpectator,
                PlayerUid = playerInfo.PlayerId,
                TeamId = playerInfo.TeamId,
                LadderRanking = playerInfo.LadderRanking,
                IsInOfficialMode = playerInfo.IsInOfficialMode,
            };

            return false;
        }
    }
}