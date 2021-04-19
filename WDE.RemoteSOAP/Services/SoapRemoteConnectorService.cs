﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.RemoteSOAP.Helpers;
using WDE.RemoteSOAP.Providers;
using WDE.RemoteSOAP.Services.Soap;

namespace WDE.RemoteSOAP.Services
{
    [SingleInstance]
    [AutoRegister]
    public class SoapRemoteConnectorService : IRemoteConnectorService
    {
        private readonly IConnectionSettingsProvider connectionSettings;
        private readonly TrinitySoapClient? trinitySoapClient;

        public bool IsConnected => trinitySoapClient != null;

        public SoapRemoteConnectorService(IConnectionSettingsProvider connectionSettings,
            ISoapClientFactory soapClientFactory)
        {
            this.connectionSettings = connectionSettings;
            var settings = connectionSettings.GetSettings();

            if (settings.IsEmpty)
                trinitySoapClient = null;
            else
                trinitySoapClient = new TrinitySoapClient(soapClientFactory, settings.Host!, settings.Port!.Value, settings.User!, settings.Password!);
        }

        public async Task<string> ExecuteCommand(IRemoteCommand command)
        {
            if (trinitySoapClient == null)
                return "";

            var response = await trinitySoapClient.ExecuteCommand(command.GenerateCommand());
            return response.Message;
        }

        public async Task ExecuteCommands(IList<IRemoteCommand> commands)
        {
            if (trinitySoapClient == null)
                return;

            foreach (var cmd in Merge(commands))
            {
                await ExecuteCommand(cmd);
            }
        }

        public IList<IRemoteCommand> Merge(IList<IRemoteCommand> commands)
        {
            return RemoteCommandMerger.Merge(commands);
        }
    }
}