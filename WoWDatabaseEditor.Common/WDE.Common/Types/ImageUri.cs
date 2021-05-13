using System;
using System.Reflection;

namespace WDE.Common.Types
{
    public readonly struct ImageUri
    {
        public readonly Assembly? Assembly;
        public readonly string Uri;
        
        public ImageUri(Assembly assembly, string relativePath)
        {
            Assembly = assembly;
            Uri = relativePath;
        }
        
        public ImageUri(string relativePath)
        {
            Assembly = null;
            Uri = relativePath;
        }
        
        public bool Equals(ImageUri other)
        {
            return Equals(Assembly, other.Assembly) && Uri == other.Uri;
        }

        public override bool Equals(object? obj)
        {
            return obj is ImageUri other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Assembly, Uri);
        }
    }
}