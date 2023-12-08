using System;
using System.Collections.Generic;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.AppVersion
{
    [AutoRegister]
    [SingleInstance]
    public class ApplicationReleaseConfiguration : BaseApplicationReleaseConfiguration
    {
        public ApplicationReleaseConfiguration(IFileSystem fileSystem) : base(fileSystem)
        {
        }
    }
}