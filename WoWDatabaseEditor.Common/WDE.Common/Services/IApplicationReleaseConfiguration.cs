namespace WDE.Common.Services
{
    public interface IApplicationReleaseConfiguration
    {
        string? GetString(string key);
        bool? GetBool(string key);
        int? GetInt(string key);
    }
}