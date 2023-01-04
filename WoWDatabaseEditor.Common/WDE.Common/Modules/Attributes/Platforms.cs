using System;

namespace WDE.Module.Attributes;

[Flags]
public enum Platforms
{
    None = 0,
    Desktop = 1,
    Browser = 2,
    Android = 4,
    iOS = 8,
    Mobile = Android | iOS,
        
    Any = Desktop | Browser | Mobile,
    NonBrowser = Desktop | Mobile,
    NonDesktop = Browser | Mobile,
    NonMobile = Desktop | Browser,
}