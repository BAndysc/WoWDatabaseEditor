namespace WDE.Common.Settings;

public interface IBoolGenericSetting : IGenericSetting
{
    bool Value { get; set; }
}