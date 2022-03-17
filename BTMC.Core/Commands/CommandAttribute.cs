using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTMC.Core.Commands
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; set; }

        /// <summary>
        /// Comma separated list of aliases
        /// </summary>
        /// <example>
        /// <code>list,all,online</code>
        /// </example>
        public string Alias { get; set; }

        public CommandAttribute(string Name)
        {
            this.Name = Name;
        }
    }
}
