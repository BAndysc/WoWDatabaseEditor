using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using WDE.Common.Profiles;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.Updater.Services
{
    [SingleInstance]
    [AutoRegister]
    public class StandaloneUpdater : IStandaloneUpdater
    {
        private readonly IAutoUpdatePlatformService platform;
        private readonly IFileSystem fs;
        private readonly IMessageBoxService messageBoxService;

        public StandaloneUpdater(IAutoUpdatePlatformService platform, 
            IFileSystem fs,
            IMessageBoxService messageBoxService)
        {
            this.platform = platform;
            this.fs = fs;
            this.messageBoxService = messageBoxService;
        }

        public bool HasPendingUpdate()
        {
            return platform.PlatformSupportsSelfInstall && fs.Exists(platform.UpdateZipFilePath);
        }

        public void RenameIfNeeded()
        {
            if (!platform.PlatformSupportsSelfInstall)
                return;
            
            RenameIfExist("_Updater", "Updater");
            RenameIfExist("_Updater.dll", "Updater.dll");
            RenameIfExist("_Updater.pdb", "Updater.pdb");
            RenameIfExist("_Updater.exe", "Updater.exe");
        }

        private void RenameIfExist(string oldName, string newName)
        {
            if (File.Exists(oldName))
            {
                if (File.Exists(newName))
                    File.Delete(newName);
                File.Move(oldName, newName);
            }
        }

        public bool Launch()
        {
            if (!platform.PlatformSupportsSelfInstall)
                return false;

            if (!TryLaunch("Updater.exe"))
                return TryLaunch("Updater");

            return true;
        }
        
        private bool TryLaunch(string file)
        {
            if (File.Exists(file))
            {
                try
                {
                    Process.Start(file, GlobalApplication.Arguments);
                    return true;
                }
                catch (Exception e)
                {
                    messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Updater")
                        .SetMainInstruction("Error while starting the updater")
                        .SetContent("While trying to start the updater, following error occured: " + e.Message +
                                    ".\n\nYou can try to run the Updater.exe (Updater on Linux) manually")
                        .WithOkButton(true)
                        .Build()).ListenErrors();
                }
            }

            return false;
        }
    }
}