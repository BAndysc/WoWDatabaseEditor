using System;

namespace WDE.Module.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ModuleRequiresCoreAttribute : Attribute 
    {
        public readonly string[] cores;
        public ModuleRequiresCoreAttribute(params string[] cores) { this.cores = cores; }
    }
}