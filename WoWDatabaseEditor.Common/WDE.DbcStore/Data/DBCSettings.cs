using WDE.Common.Services;

namespace WDE.DbcStore.Data
{
    public struct DBCSettings : ISettings
    {
        public DBCVersions DBCVersion;
        public string Path;
        public bool SkipLoading;
        public DBCLocales DBCLocale;
    }
}