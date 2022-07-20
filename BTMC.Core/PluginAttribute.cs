using System;

namespace BTMC.Core
{
    [JetBrains.Annotations.MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginAttribute : Attribute
    {
        public string Name { get; set; }
        public string Version { get; set; }

        public PluginAttribute(string name, string version)
        {
            Name = name;
            Version = version;
        }
    }
}