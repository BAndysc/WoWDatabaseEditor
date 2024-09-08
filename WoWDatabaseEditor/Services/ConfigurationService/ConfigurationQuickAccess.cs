using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prism.Commands;
using WDE.Common;
using WDE.Common.QuickAccess;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.ConfigurationService;

[AutoRegister]
public class ConfigurationQuickAccess : IQuickAccessSearchProvider
{
    private readonly IQuickCommands quickCommands;
    private readonly Lazy<IConfigureService> configService;
    private readonly List<IConfigurable> configs;
    public int Order => 600;

    private DelegateCommand<IConfigurable> openSettingsCommand;

    public ConfigurationQuickAccess(IEnumerable<IConfigurable> configs,
        IQuickCommands quickCommands,
        Lazy<IConfigureService> configService)
    {
        this.quickCommands = quickCommands;
        this.configService = configService;
        openSettingsCommand = new DelegateCommand<IConfigurable>(configPane =>
        {
            configService.Value.ShowSettings(configPane);
            quickCommands.CloseSearchCommand.Execute(null);
        });
        this.configs = configs.ToList();
    }

    public async Task Provide(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken)
    {
        foreach (var config in configs)
        {
            if (config.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                produce(new QuickAccessItem(new ImageUri("Icons/settings.png"), config.Name, "Settings",
                    config.Group == ConfigurableGroup.Basic ? "Basic settings" : "Advanced settings", openSettingsCommand, config));
            }
        }
    }
}