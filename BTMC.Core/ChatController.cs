using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BTMC.Core.Commands;
using GbxRemoteNet;
using GbxRemoteNet.XmlRpc;
using GbxRemoteNet.XmlRpc.Types;
using Microsoft.Extensions.Logging;

namespace BTMC.Core
{
    public class PlayerInfo
    {
        public string Login { get;set; }
        public string NickName { get;set; }
        public int PlayerId { get;set; }
        public int TeamId { get;set; }
        public bool IsSpectator { get;set; }
        public bool IsInOfficialMode { get;set; }
        public int LadderRanking { get;set; }
    }
    
    public class BetterChatJson
    {
        [JsonPropertyName("login")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Login { get; set; }
        
        [JsonPropertyName("nickname")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Nickname { get; set; }
        
        [JsonPropertyName("clubtag")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ClubTag { get; set; }
        
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Text { get; set; }
    }

    [Plugin("ChatController", "0.0.1")]
    public class ChatController
    {
        private readonly ILogger<ChatController> _logger;
        private readonly GbxRemoteClient _client;
        private readonly List<string> _betterChatLogins;
        private readonly List<string> _normalChatLogins;

        private bool _chatEnabled = false;

        public ChatController(ILogger<ChatController> logger, GbxRemoteService gbxRemoteService)
        {
            _logger = logger;
            _client = gbxRemoteService.Client;
            _betterChatLogins = new List<string>();
            _normalChatLogins = new List<string>();
            // TODO: On a future Load event fetch all players currently on the server and add them to the list
        }

        public async Task SendMessageAsync(string message, string senderlogin = null, string nickname = null, string clubtag = null)
        {
            var msg = new BetterChatJson
            {
                Text = message.Trim(),
                Login = senderlogin,
                Nickname = nickname,
                ClubTag = clubtag,
            };
            
            if (_betterChatLogins.Count > 0)
            {
                await _client.ChatSendServerMessageToLoginAsync("CHAT_JSON:" + JsonSerializer.Serialize(msg), string.Join(',', _betterChatLogins));
            }

            if (_normalChatLogins.Count > 0)
            {
                await _client.ChatSendServerMessageToLoginAsync(msg.Text, string.Join(',', _normalChatLogins));
            }
        }
        
        public async Task SendMessageToLoginAsync(string login, string message, string senderlogin = null, string nickname = null, string clubtag = null)
        {
            var msg = new BetterChatJson
            {
                Text = message.Trim(),
                Login = senderlogin,
                Nickname = nickname,
                ClubTag = clubtag,
            };
            
            if (_betterChatLogins.Contains(login))
            {
                await _client.ChatSendServerMessageToLoginAsync("CHAT_JSON:" + JsonSerializer.Serialize(msg), login);
            }

            if (_normalChatLogins.Contains(login))
            {
                await _client.ChatSendServerMessageToLoginAsync(msg.Text, login);
            }
        }

        [EventHandler(EventType.Load)]
        public async Task<bool> OnLoad(LoadEvent e)
        {
            var a = await _client.CallOrFaultAsync("GetPlayerList", 0, 0, 0);
            var playerList = XmlRpcTypes.ToNativeArray<PlayerInfo>((XmlRpcArray) a);
            
            _logger.LogInformation("{}", JsonSerializer.Serialize(playerList, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            _normalChatLogins.AddRange(playerList.Select(x => x.Login));

            return false;
        }

        [EventHandler(EventType.Chat)]
        public async Task<bool> OnChat(PlayerChatEvent e)
        {
            if (!_chatEnabled) return false;
            
            var a = await _client.CallOrFaultAsync("GetPlayerInfo", e.Login, 0);
            var playerInfo = (PlayerInfo)XmlRpcTypes.ToNativeValue<PlayerInfo>(a);

            var msg = new BetterChatJson
            {
                Login = e.Login,
                Nickname = playerInfo.NickName,
                Text = e.Message
            };

            if (_betterChatLogins.Count > 0)
            {
                await _client.ChatSendServerMessageToLoginAsync("CHAT_JSON:" + JsonSerializer.Serialize(msg), string.Join(',', _betterChatLogins));
            }

            if (_normalChatLogins.Count > 0)
            {
                await _client.ChatSendServerMessageToLoginAsync("[$<" + playerInfo.NickName + "$>] " + e.Message.Trim(), string.Join(',', _normalChatLogins));
            }

            return true;
        }

        [EventHandler(EventType.Join)]
        public Task<bool> OnJoin(PlayerJoinEvent e)
        {
            if (!_normalChatLogins.Contains(e.Login))
            {
                _normalChatLogins.Add(e.Login);
            }

            return Task.FromResult(false);
        }
        
        [EventHandler(EventType.Disconnect)]
        public Task<bool> OnDisconnect(PlayerDisconnectEvent e)
        {
            if (_betterChatLogins.Contains(e.Login))
            {
                _betterChatLogins.Remove(e.Login);
            }
            else if (_normalChatLogins.Contains(e.Login))
            {
                _normalChatLogins.Remove(e.Login);
            }

            return Task.FromResult(false);
        }
        
        [Command("chat")]
        public async Task ChatCommand(CommandArgs args)
        {
            if (args.Args.Length > 1)
            {
                await _client.ChatSendServerMessageToLoginAsync("Usage: /chat [on/off]", args.PlayerLogin);
                return;
            }

            if (args.Args.Length == 1 && args.Args[0] == "on")
            {
                await _client.CallOrFaultAsync("ChatEnableManualRouting", true, false);
                _chatEnabled = true;
            }
            else if (args.Args.Length == 1 && args.Args[0] == "off")
            {
                await _client.CallOrFaultAsync("ChatEnableManualRouting", false, false);
                _chatEnabled = false;
            }
            
            await _client.ChatSendServerMessageToLoginAsync($"Chat is {(_chatEnabled ? "on" : "off")}", args.PlayerLogin);
        }
        
        [Command("chatformat")]
        public async Task ChatFormatCommand(CommandArgs args)
        {
            if (args.Args.Length != 1)
            {
                await _client.ChatSendServerMessageToLoginAsync("Usage: /chatformat [text/json]", args.PlayerLogin);
                return;
            }

            if (args.Args[0] == "text")
            {
                if (!_betterChatLogins.Contains(args.PlayerLogin))
                {
                    return;
                }
                _betterChatLogins.Remove(args.PlayerLogin);
                _normalChatLogins.Add(args.PlayerLogin);
            }
            else if (args.Args[0] == "json")
            {
                if (_betterChatLogins.Contains(args.PlayerLogin))
                {
                    return;
                }
                _betterChatLogins.Add(args.PlayerLogin);
                _normalChatLogins.Remove(args.PlayerLogin);
            }
            else
            {
                await _client.ChatSendServerMessageToLoginAsync("Usage: /chatformat [text/json]", args.PlayerLogin);
            }
        }
    }
}