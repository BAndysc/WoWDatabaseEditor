using System;

namespace WDE.Module.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ModuleBlocksOtherAttribute : Attribute 
    {
        public readonly string otherModule;
        public ModuleBlocksOtherAttribute(string otherModule) { this.otherModule = otherModule; }
    }
}