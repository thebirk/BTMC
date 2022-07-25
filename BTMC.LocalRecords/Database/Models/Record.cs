using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTMC.LocalRecords.Database.Models
{
    public class Record
    {
        [Key]
        public int RecordId { get; set; }
        public int Time { get; set; }
        public string PlayerLogin { get; set; }

        public string MapId { get; set; }
        public Map Map { get; set; }
    }
}
