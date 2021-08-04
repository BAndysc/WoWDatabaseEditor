using System;
using System.Diagnostics;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Http
{
    [UniqueProvider]
    public interface IUrlOpenService
    {
        void OpenUrl(string url);
    }
    
    [SingleInstance]
    [AutoRegister]
    public class UrlOpenService : IUrlOpenService
    {
        public void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) {UseShellExecute = true});
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}