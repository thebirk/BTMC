using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTMC.LocalRecords.Database.Models
{
    public class Record
    {
        public int RecordId { get; set; }
        public int Time { get; set; }
        public string PlayerUid { get; set; }

        public int MapId { get; set; }
        public Map Map { get; set; }
    }
}
