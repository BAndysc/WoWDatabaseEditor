using System.Collections.Generic;
using System.Linq;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.ModulesManagement;

[SingleInstance]
[AutoRegister]
public class GlobalServiceRoot : IGlobalServiceRoot
{
    private readonly List<IGlobalService> globalService;

    public GlobalServiceRoot(IEnumerable<IGlobalService> globalServices)
    {
        this.globalService = globalServices.ToList();
    }
    
    public void Dispose()
    {
        foreach (var service in globalService)
        {
            if (service is System.IDisposable disposable)
                disposable.Dispose();
        }   
    }
}