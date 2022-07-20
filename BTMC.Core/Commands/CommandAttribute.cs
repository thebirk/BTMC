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
        public string[] Aliases { get; set; }

        public CommandAttribute(string name)
        {
            Name = name;
        }

        public CommandAttribute(string name, params string[] aliases)
        {
            Name = name;
            Aliases = aliases;
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
