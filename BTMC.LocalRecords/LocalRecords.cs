using BTMC.Core;
using BTMC.Core.Commands;
using BTMC.LocalRecords.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using BTMC.LocalRecords.Database.Models;
using GbxRemoteNet;
using Microsoft.Extensions.Configuration;

namespace BTMC.LocalRecords
{
    public enum DbType
    {
        Postgres,
    }

    [Settings("LocalRecords")]
    public class LocalRecordsSettings
    {
        public DbType DbType { get; set; } = DbType.Postgres;
        public string ConnectionString { get; set; }
    }

    [Plugin("LocalRecords", "1.0.0")]
    public class LocalRecords
    {
        private readonly ILogger<LocalRecords> _logger;
        private readonly LocalRecordsSettings _settings;
        private readonly LocalRecordsContext _context;
        private readonly GbxRemoteClient _client;
        private readonly ChatController _chatController;
        private readonly PlayerController _playerController;

        public LocalRecords(ILogger<LocalRecords> logger, IOptions<LocalRecordsSettings> options, GbxRemoteService gbxRemoteService, IConfiguration configuration, ChatController chatController, PlayerController playerController)
        {
            _logger = logger;
            _client = gbxRemoteService.Client;
            _chatController = chatController;
            _playerController = playerController;

            _settings = new LocalRecordsSettings();
            configuration.Bind("localrecords", _settings);

            _context = new LocalRecordsContext(_settings);
            _context.Database.EnsureCreated();
        }

        [EventHandler(EventType.Unload)]
        public async Task<bool> Unload(UnloadEvent e)
        {
            await _context.SaveChangesAsync();
            await _context.DisposeAsync();

            return false;
        }

        [EventHandler(EventType.Finish)]
        public async Task<bool> OnFinish(FinishEvent args)
        {
            var mapInfo = await _client.GetCurrentMapInfoAsync();

            var map = await _context.Maps.SingleOrDefaultAsync(x => x.MapId == mapInfo.UId);
            if (map == null)
            {
                map = new Map
                {
                    Name = mapInfo.Name,
                    MapId = mapInfo.UId,
                };
                await _context.Maps.AddAsync(map);
            }

            // player incoherence? does that event apply in 2020?
            // does it ignore finishes automatically or do we have to track giveups/finishes for incoherent users
            var record = await _context.Records.SingleOrDefaultAsync(x => x.MapId == map.MapId && x.PlayerLogin == args.Login);
            if (record != null)
            {
                if (args.RaceTime >= record.Time)
                {
                    return false;
                }

                var oldTime = record.Time;
                record.Time = args.RaceTime;
                
                await _chatController.SendMessageAsync($"{_playerController.GetPlayerInfo(args.Login).Nickname} set a new personal best! {TimeSpan.FromMilliseconds(args.RaceTime):hh\\:mm\\:ss\\.fff}(-{TimeSpan.FromMilliseconds(oldTime - args.RaceTime):hh\\:mm\\:ss\\.fff})", clubtag: "LocalRecords");
            }
            else
            {
                await _context.Records.AddAsync(new Record
                {
                    MapId = map.MapId,
                    Time = args.RaceTime,
                    PlayerLogin = args.Login,
                });
                
                await _chatController.SendMessageAsync($"{_playerController.GetPlayerInfo(args.Login).Nickname} set a new personal best! {TimeSpan.FromMilliseconds(args.RaceTime):hh\\:mm\\:ss\\.fff}", clubtag: "LocalRecords");
            }
            
            await _context.SaveChangesAsync();

            return false;
        }
    }

    [Command("toprecs", "recs")]
    public class TopRecsCommand : CommandBase
    {
        // All loaded Plugins gets registered as singleton services
        private readonly LocalRecords _plugin;
        private readonly ILogger<TopRecsCommand> _logger;

        public TopRecsCommand(LocalRecords plugin, ILogger<TopRecsCommand> logger)
        {
            _plugin = plugin;
            _logger = logger;
        }

        public override Task ExecuteAsync()
        {
            _logger.LogInformation("Hello world!");
            return Task.CompletedTask;
        }
    }
}