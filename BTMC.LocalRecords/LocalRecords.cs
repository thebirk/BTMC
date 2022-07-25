using BTMC.Core;
using BTMC.Core.Commands;
using BTMC.LocalRecords.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using GbxRemoteNet;

namespace BTMC.LocalRecords
{
    public enum DbType
    {
        Postgres,
    }

    [Settings("LocalRecords")]
    public class LocalRecordsSettings
    {
        public DbType DbType { get; set; }
        public string ConnectionString { get; set; }
    }

    [Plugin("LocalRecords", "1.0.0")]
    public class LocalRecords
    {
        private readonly ILogger<LocalRecords> _logger;
        private readonly LocalRecordsSettings _settings;
        private readonly LocalRecordsContext _context;
        private readonly GbxRemoteClient _client;

        public LocalRecords(ILogger<LocalRecords> logger, IOptions<LocalRecordsSettings> options, GbxRemoteService gbxRemoteService)
        {
            _logger = logger;
            _settings = options?.Value;
            _client = gbxRemoteService.Client;

            _logger.LogInformation("LocalRecords constructor");
            _context = new LocalRecordsContext(_settings);
            _context.Database.EnsureCreated();
        }

        public void Unload()
        {
            _context.SaveChanges();
            _context.Dispose();
        }

        [EventHandler(EventType.Chat)]
        public void PlayerChat(PlayerChatEvent e)
        {
            _logger.LogInformation($"PlayerChat: {e.PlayerUid} ({e.Login}): {e.Message}");
        }

        [EventHandler(EventType.Finish)]
        public async Task<bool> OnFinish(FinishEvent args)
        {
            var mapInfo = await _client.GetCurrentMapInfoAsync();
            
            _context.Records.Add(new Database.Models.Record
            {
                MapId = mapInfo.UId,
                Time = args.RaceTime,
                PlayerLogin = args.Login,
            });
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
