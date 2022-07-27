using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BTMC.Core;
using BTMC.Core.Commands;
using GbxRemoteNet;
using GbxRemoteNet.Structs;
using GbxRemoteNet.XmlRpc;
using GbxRemoteNet.XmlRpc.Types;
using Microsoft.Extensions.Logging;

namespace BTMC.TestPlugin
{
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
        private readonly GbxRemoteClient _client;
        private readonly PlayerController _playerController;

        public TestPlugin(ILogger<TestPlugin> logger, ChatController chatController, ManialinkController manialinkController, GbxRemoteService gbxRemoteService, PlayerController playerController)
        {
            _logger = logger;
            _chatController = chatController;
            _manialinkController = manialinkController;
            _client = gbxRemoteService.Client;
            _playerController = playerController;
        }

        [Command("map")]
        public async Task MapCommand(CommandArgs args)
        {
            var mapInfo = await _client.GetCurrentMapInfoAsync();
            await _chatController.SendMessageToLoginAsync(args.PlayerLogin, JsonSerializer.Serialize(mapInfo, new JsonSerializerOptions { WriteIndented = true }));
        }

        [Command("simple")]
        public async Task SimpleCommand(CommandArgs args)
        {
            var okAction = _manialinkController.GenUniqueAction();
            var cancelAction = _manialinkController.GenUniqueAction();
            var answer = await _manialinkController.SendManialinkAsync(
                args.PlayerLogin,
                $@"
                <manialink version=""3"">
                <label pos=""0 -10"" action=""{okAction}"" text=""Ok"" />
                <label pos=""0  10"" action=""{cancelAction}"" text=""Cancel"" />
                </manialink>
                ",
                new[] {okAction, cancelAction}
            );
            
            await _chatController.SendMessageToLoginAsync(args.PlayerLogin, $"You clicked {(answer == okAction ? "Ok" : "Cancel")}!");
        }

        [EventHandler(EventType.Checkpoint)]
        public async Task<bool> OnCheckpoint(CheckpointEvent e)
        {
            await _chatController.SendMessageToLoginAsync(e.Login, $"CP: {e.CheckpointInRace + 1}, SPEED: {e.Speed*3.6:F0}", clubtag: "TIMER");

            return false;
        }

        [EventHandler(EventType.Finish)]
        public async Task<bool> OnFinish(FinishEvent e)
        {
            await _chatController.SendMessageToLoginAsync(e.Login, $"FINISH, TIME: {TimeSpan.FromMilliseconds(e.RaceTime):hh\\:mm\\:ss\\.fff}, SPEED: {e.Speed*3.6:F0}", clubtag: "TIMER");

            return false;
        }

        [EventHandler(EventType.Join)]
        public async Task<bool> OnJoin(PlayerJoinEvent e)
        {
            var playerInfo = _playerController.GetPlayerInfo(e.Login);
            await _chatController.SendMessageAsync($"{playerInfo.Nickname} has {(e.IsSpectator ? "joined as a spectator" : "joined the server")}");

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
                await _chatController.SendMessageToLoginAsync(PlayerLogin, "You do not have access to this command", clubtag: "NOTICE");
                return;
            }
            
            if (Args.Length == 0)
            {
                await _chatController.SendMessageToLoginAsync(PlayerLogin, "Usage: /notice <message>", clubtag: "NOTICE");
                return;
            }
            
            await _chatController.SendMessageAsync(Args[0], clubtag: "NOTICE");
        }
    }
}