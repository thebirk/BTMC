using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GbxRemoteNet;

namespace BTMC.Core.Commands
{
    [JetBrains.Annotations.MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
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

    public class CommandArgs
    {
        public GbxRemoteClient Client { get; set; }
        public string PlayerLogin { get; set; }
        public int PlayerUid { get; set; }
        public string[] Args { get; set; }
    }

    public delegate Task BasicCommandHandler(CommandArgs args);
}
