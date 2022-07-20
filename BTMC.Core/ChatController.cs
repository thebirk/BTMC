using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BTMC.Core.Commands;
using GbxRemoteNet;
using GbxRemoteNet.XmlRpc;
using Microsoft.Extensions.Logging;

namespace BTMC.Core
{
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
    
    public class BetterChatJson
    {
        [JsonPropertyName("login")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Login { get; set; }
        [JsonPropertyName("nickname")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Nickname { get; set; }
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Text { get; set; }
    }
    
    [Plugin("ChatController", "0.0.1")]
    public class ChatController
    {
        private readonly ILogger<ChatController> _logger;
        private readonly List<string> _betterChatLogins;
        private readonly List<string> _normalChatLogins;

        private bool _chatEnabled = false;

        public ChatController(ILogger<ChatController> logger)
        {
            _logger = logger;
            _betterChatLogins = new List<string>();
            _normalChatLogins = new List<string>();
            // TODO: On a future Load event fetch all players currently on the server and add them to the list
        }

        public async Task SendMessage(GbxRemoteClient client, string message)
        {
            var msg = new BetterChatJson
            {
                Text = message.Trim()
            };
            
            if (_betterChatLogins.Count > 0)
            {
                await client.ChatSendServerMessageToLoginAsync("CHAT_JSON:" + JsonSerializer.Serialize(msg), string.Join(',', _betterChatLogins));
            }

            if (_normalChatLogins.Count > 0)
            {
                await client.ChatSendServerMessageToLoginAsync(msg.Text, string.Join(',', _normalChatLogins));
            }
        }
        
        public async Task SendMessageToLogin(GbxRemoteClient client, string login, string message)
        {
            var msg = new BetterChatJson
            {
                Text = message.Trim()
            };
            
            if (_betterChatLogins.Contains(login))
            {
                await client.ChatSendServerMessageToLoginAsync("CHAT_JSON:" + JsonSerializer.Serialize(msg), login);
            }

            if (_normalChatLogins.Contains(login))
            {
                await client.ChatSendServerMessageToLoginAsync(msg.Text, login);
            }
        }

        [EventHandler(EventType.Chat)]
        public async Task<bool> OnChat(PlayerChatEvent e)
        {
            if (e.Handled) return false;
            if (!_chatEnabled) return false;
            
            var a = await e.Client.CallOrFaultAsync("GetPlayerInfo", e.Login, 0);
            var playerInfo = (PlayerInfo)XmlRpcTypes.ToNativeValue<PlayerInfo>(a);
            var msg = new BetterChatJson
            {
                Login = e.Login,
                Nickname = playerInfo.NickName,
                Text = e.Message
            };

            if (_betterChatLogins.Count > 0)
            {
                await e.Client.ChatSendServerMessageToLoginAsync("CHAT_JSON:" + JsonSerializer.Serialize(msg), string.Join(',', _betterChatLogins));
            }

            if (_normalChatLogins.Count > 0)
            {
                await e.Client.ChatSendServerMessageToLoginAsync(playerInfo.NickName + "$g$z: " + e.Message.Trim(), string.Join(',', _normalChatLogins));
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
                await args.Client.ChatSendServerMessageToLoginAsync("Usage: /chat [on/off]", args.PlayerLogin);
                return;
            }

            if (args.Args.Length == 1 && args.Args[0] == "on")
            {
                await args.Client.CallOrFaultAsync("ChatEnableManualRouting", true, false);
                _chatEnabled = true;
            }
            else if (args.Args.Length == 1 && args.Args[0] == "off")
            {
                await args.Client.CallOrFaultAsync("ChatEnableManualRouting", false, false);
                _chatEnabled = false;
            }
            
            await args.Client.ChatSendServerMessageToLoginAsync($"Chat is {(_chatEnabled ? "on" : "off")}", args.PlayerLogin);
        }
        
        [Command("chatformat")]
        public async Task ChatFormatCommand(CommandArgs args)
        {
            if (args.Args.Length != 1)
            {
                await args.Client.ChatSendServerMessageToLoginAsync("Usage: /chatformat [text/json]", args.PlayerLogin);
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
                await args.Client.ChatSendServerMessageToLoginAsync("Usage: /chatformat [text/json]", args.PlayerLogin);
            }
        }
    }
}