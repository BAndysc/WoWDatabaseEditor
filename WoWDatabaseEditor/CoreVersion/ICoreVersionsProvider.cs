using System.Collections.Generic;
using WDE.Common.CoreVersion;

namespace WoWDatabaseEditor.CoreVersion
{
    public interface ICoreVersionsProvider
    {
        IEnumerable<ICoreVersion> AllVersions { get; }
    }
    
    public interface ICoreVersionSettings
    {
        void UpdateCurrentVersion(ICoreVersion version);
    }
}