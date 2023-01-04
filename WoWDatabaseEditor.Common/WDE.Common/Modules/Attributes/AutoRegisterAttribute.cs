using System;

namespace WDE.Module.Attributes
{
    public class AutoRegisterAttribute : Attribute
    {
        public Platforms RequiredPlatforms { get; }
        
        public AutoRegisterAttribute() {}
        
        public AutoRegisterAttribute(Platforms requiredPlatforms)
        {
            RequiredPlatforms = requiredPlatforms;
        }
    }
}