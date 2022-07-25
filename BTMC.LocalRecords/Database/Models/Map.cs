using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTMC.LocalRecords.Database.Models
{
    public class Map
    {
        [Key]
        public string MapId { get; set; }
        public string Name { get; set; }

        public IEnumerable<Record> Records { get; set; }
    }
}
