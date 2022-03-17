using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTMC.LocalRecords
{
    class Program
    {
        static void Main(string[] args)
        {
            var localRecords = new LocalRecords(
                LoggerFactory.Create(config => { config.AddConsole();  }).CreateLogger<LocalRecords>(),
                Options.Create(new LocalRecordsSettings {
                    DbType = DbType.Postgres,
                }
            ));
        }
    }
}
