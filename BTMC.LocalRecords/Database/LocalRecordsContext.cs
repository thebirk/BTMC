using BTMC.Core;
using BTMC.LocalRecords.Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BTMC.LocalRecords.Database
{
    public class LocalRecordsContext : DbContext
    {
        public DbSet<Record> Records { get; set; }
        public DbSet<Map> Maps { get; set; }

        private readonly LocalRecordsSettings _settings;

        public LocalRecordsContext(LocalRecordsSettings settings)
        {
            _settings = settings;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Record>()
                .HasIndex(x => new { x.MapId, x.PlayerLogin })
                .IsUnique();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            switch (_settings.DbType)
            {
                case DbType.Postgres:
                    optionsBuilder.UseNpgsql(_settings.ConnectionString);
                    break;
                default:
                    throw new Exception("Invalid DbType configured");
            }
        }
    }
}