using System;
using System.Reflection;

namespace WDE.Common.Types
{
    public readonly struct ImageUri
    {
        public string? Uri { get; }
        
        public ImageUri(string uri)
        {
            Uri = uri;
        }

        public static ImageUri Empty { get; } = new ImageUri("Icons/empty.png");

        public static bool operator ==(ImageUri left, ImageUri right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImageUri left, ImageUri right)
        {
            return !left.Equals(right);
        }

        public bool Equals(ImageUri other)
        {
            return Uri == other.Uri;
        }

        public override bool Equals(object? obj)
        {
            return obj is ImageUri other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Uri?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return Uri ?? "(-)";
        }
    }
}
