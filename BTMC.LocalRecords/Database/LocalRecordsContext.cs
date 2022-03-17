using BTMC.Core;
using BTMC.LocalRecords.Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTMC.LocalRecords.Database
{
    class LocalRecordsContext : DbContext
    {
        public DbSet<Record> Records { get; set; }
        public DbSet<Map> Maps { get; set; }

        private readonly LocalRecordsSettings _settings;

        public LocalRecordsContext(LocalRecordsSettings settings)
        {
            _settings = settings;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            switch (_settings.DbType)
            {
                case DbType.Postgres:
                    optionsBuilder.UseNpgsql("<REDACTED>");
                    break;
                default:
                    throw new Exception("Invalid DbType configured");
            }
        }
    }
}
