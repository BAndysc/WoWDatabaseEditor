using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Services;
using WDE.Common.Types;

namespace WDE.Common.Avalonia.Components;

public class WdeIconPicker : TemplatedControl
{
    public static readonly StyledProperty<string?> IconPathProperty = AvaloniaProperty.Register<WdeIconPicker, string?>(nameof(IconPath), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<bool> IsNonNullIconProperty = AvaloniaProperty.Register<WdeIconPicker, bool>(nameof(IsNonNullIcon), defaultBindingMode: BindingMode.TwoWay);

    public string? IconPath
    {
        get => GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }

    public bool IsNonNullIcon
    {
        get => GetValue(IsNonNullIconProperty);
        set => SetValue(IsNonNullIconProperty, value);
    }

    static WdeIconPicker()
    {
        IconPathProperty.Changed.AddClassHandler<WdeIconPicker>((x, _) => x.UpdateIsNull());
        IsNonNullIconProperty.Changed.AddClassHandler<WdeIconPicker>((x, _) => x.UpdateIcon());
    }

    private bool inEvent;
    private string lastNonNullIconPath = "";

    private void UpdateIsNull()
    {
        if (inEvent)
            return;

        inEvent = true;
        SetCurrentValue(IsNonNullIconProperty, IconPath != null);
        inEvent = false;
    }

    private void UpdateIcon()
    {
        if (inEvent)
            return;

        inEvent = true;
        if (IsNonNullIcon && IconPath == null)
        {
            SetCurrentValue(IconPathProperty, lastNonNullIconPath);
        }
        else if (!IsNonNullIcon)
        {
            lastNonNullIconPath = IconPath ?? "";
            SetCurrentValue(IconPathProperty, null);
        }
        inEvent = false;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        var downloadMoreIconsButton = e.NameScope.Get<Button>("DownloadMoreIconsButton");
        var service = ViewBind.ResolveViewModel<IIconFinderService>();
        downloadMoreIconsButton.IsVisible = service.Enabled;
        downloadMoreIconsButton.Click += async (sender, args) =>
        {
            var image = await service.PickIconAsync();
            if (image.HasValue)
            {
                SetCurrentValue(IconPathProperty, image.Value.Uri);
            }
        };
    }
}

public static class DefinitionEditorStatic
{
    public static IEnumerable<string> Icons { get; }

    static DefinitionEditorStatic()
    {
        var icons = new List<string>();

        if (!OperatingSystem.IsBrowser())
        {
            var files = Directory.GetFiles("Icons/", "*.png");
            foreach (var file in files)
            {
                if (file.Contains("@2x"))
                    continue;

                if (file.Contains("_big"))
                    continue;

                if (file.Contains("_dark"))
                    continue;

                string relativePath;
                if (file.Contains("Icons/"))
                    relativePath = file.Substring(file.IndexOf("Icons/", StringComparison.Ordinal));
                else if (file.Contains("Icons\\"))
                    relativePath = file.Substring(file.IndexOf("Icons\\", StringComparison.Ordinal));
                else
                    continue;

                icons.Add(relativePath);
            }
        }

        Icons = icons;
    }
}