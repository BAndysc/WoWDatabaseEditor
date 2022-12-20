using System.Collections.Generic;

namespace WDE.Common.Settings;

public interface IListOptionGenericSetting : IGenericSetting
{
    IReadOnlyList<object> Options { get; }
    object SelectedOption { get; set; }
}