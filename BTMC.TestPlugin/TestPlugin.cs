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
    
    [Settings("test")]
    public class TestSettings
    {
    
    }

    [Plugin("Test Plugin", "0.0.1")]
    public class TestPlugin
    {
        private readonly ILogger<TestPlugin> _logger;
        private readonly ChatController _chatController;
        private readonly ManialinkController _manialinkController;

        private readonly int _okAction;
        private readonly int _cancelAction;

        public TestPlugin(ILogger<TestPlugin> logger, ChatController chatController, ManialinkController manialinkController)
        {
            _logger = logger;
            _chatController = chatController;
            _manialinkController = manialinkController;

            _okAction = _manialinkController.GenUniqueAction();
            _cancelAction = _manialinkController.GenUniqueAction();
        }

        [Command("simple")]
        public async Task SimpleCommand(CommandArgs args)
        {
            await args.Client.ChatSendServerMessageToLoginAsync("simple command :)", args.PlayerLogin);
            
            var answer = await _manialinkController.SendManialinkAsync(
                args.Client, args.PlayerLogin,
                $@"
                <manialink version=""3"">
                <label pos=""0 -10"" action=""{_okAction}"" text=""Ok"" />
                <label pos=""0  10"" action=""{_cancelAction}"" text=""Cancel"" />
                </manialink>
                ",
                new[] {_okAction, _cancelAction}
            );
            await _chatController.SendMessageToLoginAsync(args.Client, args.PlayerLogin, $"You clicked {(answer == _okAction ? "Ok" : "Cancel")}!");
        }

        [EventHandler(EventType.Checkpoint)]
        public async Task<bool> OnCheckpoint(CheckpointEvent e)
        {
            await _chatController.SendMessageToLoginAsync(e.Client, e.Login, $"CP: {e.CheckpointInRace + 1}, SPEED: {e.Speed*3.6:F0}", clubtag: "TIMER");

            return false;
        }

        [EventHandler(EventType.Finish)]
        public async Task<bool> OnFinish(FinishEvent e)
        {
            await _chatController.SendMessageToLoginAsync(e.Client, e.Login, $"FINISH, TIME: {TimeSpan.FromMilliseconds(e.RaceTime):hh\\:mm\\:ss\\.fff}, SPEED: {e.Speed*3.6:F0}", clubtag: "TIMER");

            return false;
        }

        [EventHandler(EventType.Join)]
        public async Task<bool> OnJoin(PlayerJoinEvent e)
        {
            _logger.LogInformation("login: {} , isSpectator: {}", e.Login, e.IsSpectator);
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
                await _chatController.SendMessageToLoginAsync(Client, PlayerLogin, "You do not have access to this command", clubtag: "NOTICE");
                return;
            }
            
            if (Args.Length == 0)
            {
                await _chatController.SendMessageToLoginAsync(Client, PlayerLogin, "Usage: /notice <message>", clubtag: "NOTICE");
                return;
            }
            
            await _chatController.SendMessageAsync(Client, Args[0], clubtag: "NOTICE");
        }
    }
}