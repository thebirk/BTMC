using BTMC.Core;
using BTMC.Core.Commands;
using BTMC.LocalRecords.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BTMC.LocalRecords
{
    public enum DbType
    {
        Postgres,
    }

    [Settings]
    public class LocalRecordsSettings
    {
        public DbType DbType { get; set; }
        public string ConnectionString { get; set; }
    }

    [Plugin("LocalRecords", "1.0.0")]
    public class LocalRecords
    {
        public string Name => "LocalRecords";
        public string Version => "1.0.0";
        
        private readonly ILogger<LocalRecords> _logger;
        private readonly LocalRecordsSettings _settings;
        private readonly LocalRecordsContext _context;

        public LocalRecords(ILogger<LocalRecords> logger, IOptions<LocalRecordsSettings> options)
        {
            _logger = logger;
            _settings = options?.Value;

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
        public void OnFinish(PlayerFinishArgs args)
        {
            // get current map
            var mapId = 0;
            _context.Records.Add(new Database.Models.Record
            {
                MapId = mapId,
                Time = int.Parse(args.TimeOrScore),
                PlayerUid = args.PlayerUid
            });
            _context.SaveChanges();
        }
    }

    [Command("toprecs", Alias = "recs")]
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
