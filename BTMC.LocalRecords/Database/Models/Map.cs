using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTMC.LocalRecords.Database.Models
{
    public class Map
    {
        public int MapId { get; set; }
        public string Name { get; set; }

        public IEnumerable<Record> Records { get; set; }
    }
}
