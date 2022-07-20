using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BTMC.Core;
using BTMC.Core.Commands;
using GbxRemoteNet.XmlRpc;
using Microsoft.Extensions.Logging;

namespace BTMC.TestPlugin
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
        public string Login { get; set; }
        public string Nickname { get; set; }
        public string Text { get; set; }
    }

    [Plugin("Test Plugin", "0.0.1")]
    public class TestPlugin
    {
        private readonly ILogger<TestPlugin> _logger;
        private bool chatEnabled = false;
        private List<string> betterChatEnabled = new();

        public TestPlugin(ILogger<TestPlugin> logger)
        {
            _logger = logger;
        }

        [Command("simple")]
        public async Task SimpleCommand(CommandArgs args)
        {
            await args.Client.ChatSendServerMessageToLoginAsync("simple command :)", args.PlayerLogin);
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
                chatEnabled = true;
            }
            else if (args.Args.Length == 1 && args.Args[0] == "off")
            {
                await args.Client.CallOrFaultAsync("ChatEnableManualRouting", false, false);
                chatEnabled = false;
            }
            
            await args.Client.ChatSendServerMessageToLoginAsync($"Chat is {(chatEnabled ? "on" : "off")}", args.PlayerLogin);
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
                if (!betterChatEnabled.Contains(args.PlayerLogin))
                {
                    return;
                }
                betterChatEnabled.Remove(args.PlayerLogin);
            }
            else if (args.Args[0] == "json")
            {
                if (betterChatEnabled.Contains(args.PlayerLogin))
                {
                    return;
                }
                betterChatEnabled.Add(args.PlayerLogin);
            }
            else
            {
                await args.Client.ChatSendServerMessageToLoginAsync("Usage: /chatformat [text/json]", args.PlayerLogin);
            }
        }

        [EventHandler(EventType.Chat)]
        public async Task<bool> OnChat(PlayerChatEvent e)
        {
            // Early out if another plugin is handling chat event
            if (e.Handled) return false;
            if (!chatEnabled) return false;
            
            var a = await e.Client.CallOrFaultAsync("GetPlayerInfo", e.Login, 0);
            var playerInfo = (PlayerInfo)XmlRpcTypes.ToNativeValue<PlayerInfo>(a);
            var msg = new BetterChatJson
            {
                Login = e.Login,
                Nickname = playerInfo.NickName,
                Text = e.Message
            };

            if (betterChatEnabled.Count > 0)
            {
                await e.Client.ChatSendServerMessageToLoginAsync("CHAT_JSON:" + JsonSerializer.Serialize(msg), string.Join(',', betterChatEnabled));
            }
            await e.Client.ChatSendServerMessageAsync(playerInfo.NickName + "$g$z: " + e.Message.Trim());

            return true;
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

    [Command("notice")]
    public class NoticeCommand : CommandBase
    {
        private readonly AdminController _adminController;
        
        public NoticeCommand(AdminController adminController)
        {
            _adminController = adminController;
        }
        
        public override async Task ExecuteAsync()
        {
            if (!_adminController.IsAdmin(PlayerLogin))
            {
                await Client.ChatSendServerMessageToLoginAsync("You do not have access to this command", PlayerLogin);
                return;
            }
            
            if (Args.Length == 0)
            {
                await Client.ChatSendServerMessageToLoginAsync("Usage: /notice <message>", PlayerLogin);
                return;
            }

            await Client.ChatSendServerMessageAsync(Args[0]);
        }
    }
}