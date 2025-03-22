using System;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.RemoteSOAP.Providers;
using WDE.RemoteSOAP.Services;
using WDE.RemoteSOAP.Services.Soap;

namespace WDE.RemoteSOAP.ViewModels
{
    [AutoRegister(Platforms.Desktop)]
    public class SoapConfigViewModel : BindableBase, IFirstTimeWizardConfigurable
    {
        private readonly IConnectionSettingsProvider settings;
        private string? host;
        private string? pass;
        private string? port;
        private string? user;

        public SoapConfigViewModel(IConnectionSettingsProvider settings, ISoapClientFactory clientFactory)
        {
            port = (settings.GetSettings().Port ?? 7878).ToString();
            user = settings.GetSettings().User;
            pass = settings.GetSettings().Password;
            host = settings.GetSettings().Host ?? "127.0.0.1";
            this.settings = settings;

            Save = new DelegateCommand(() =>
            {
                int? port = null;
                if (int.TryParse(Port, out int port_))
                    port = port_;
                settings.UpdateSettings(User, Password, Host, port);
                IsModified = false;
            });

            TestConnection = new AsyncAutoCommand(async () =>
            {
                int? port = null;
                if (int.TryParse(Port, out int port_))
                    port = port_;
                var client = new TrinitySoapClient(clientFactory, host, port ?? 0, user!, pass!);
                try
                {
                    var response = await client.ExecuteCommand("server info");
                    if (response.Success)
                        TestConnectionOutput = "Success:\n" + response.Message;
                    else
                    {
                        if (string.IsNullOrEmpty(response.Message))
                            TestConnectionOutput =
                                "Server responded, but response ill-formed. Is it TrinityCore based server?";
                        else
                            TestConnectionOutput = "Server responded, but command failed: " + response.Message;
                    }
                }
                catch (Exception e)
                {
                    TestConnectionOutput = "Connection failed: " + e.Message;
                }
            }, () => !string.IsNullOrEmpty(host) && int.TryParse(port, out var _) && !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass));

            PropertyChanged += (_, _) => TestConnection.RaiseCanExecuteChanged();
        }

        public string? Host
        {
            get => host;
            set
            {
                IsModified = true;
                SetProperty(ref host, value);
            }
        }

        public string? Port
        {
            get => port;
            set
            {
                IsModified = true;
                SetProperty(ref port, value);
            }
        }

        public string? User
        {
            get => user;
            set
            {
                IsModified = true;
                SetProperty(ref user, value);
            }
        }

        public string? Password
        {
            get => pass;
            set
            {
                IsModified = true;
                SetProperty(ref pass, value);
            }
        }

        public ICommand Save { get; }

        public ImageUri Icon { get; } = new ImageUri("Icons/document_remotedesktop_big.png");
        public string Name => "Soap connector";

        public string ShortDescription =>
            "WoW Database Editor can connect to the TrinityCore-based server console and execute reload commands for you.\n\nFirstly you have to enable SOAP in worldserver configuration, set `SOAP.Enabled` to `1` in order to do so. Then put your GM account username and password to execute commands on your behalf.";

        public AsyncAutoCommand TestConnection { get; }

        private string testConnectionOutput = "";
        public string TestConnectionOutput
        {
            get => testConnectionOutput;
            set => SetProperty(ref testConnectionOutput, value);
        }
        
        private bool isModified;
        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }

        public bool IsRestartRequired => true;
        public ConfigurableGroup Group => ConfigurableGroup.Basic;
    }
}