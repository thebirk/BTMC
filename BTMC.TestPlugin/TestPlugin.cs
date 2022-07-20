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

    [Plugin("Test Plugin", "0.0.1")]
    public class TestPlugin
    {
        private readonly ILogger<TestPlugin> _logger;

        public TestPlugin(ILogger<TestPlugin> logger)
        {
            _logger = logger;
        }

        [Command("simple")]
        public async Task SimpleCommand(CommandArgs args)
        {
            await args.Client.ChatSendServerMessageToLoginAsync("simple command :)", args.PlayerLogin);
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

    [Command("notice", "n", "not")]
    public class NoticeCommand : CommandBase
    {
        private readonly AdminController _adminController;
        private readonly ChatController _chatController;
        
        public NoticeCommand(AdminController adminController, ChatController chatController)
        {
            _adminController = adminController;
            _chatController = chatController;
        }
        
        public override async Task ExecuteAsync()
        {
            if (!_adminController.IsAdmin(PlayerLogin))
            {
                await _chatController.SendMessageToLogin(Client, PlayerLogin, "You do not have access to this command");
                //await Client.ChatSendServerMessageToLoginAsync("You do not have access to this command", PlayerLogin);
                return;
            }
            
            if (Args.Length == 0)
            {
                await _chatController.SendMessageToLogin(Client, PlayerLogin, "Usage: /notice <message>");
                //await Client.ChatSendServerMessageToLoginAsync("Usage: /notice <message>", PlayerLogin);
                return;
            }

            //await Client.ChatSendServerMessageAsync(Args[0]);
            await _chatController.SendMessage(Client, Args[0]);
        }
    }
}