namespace WDE.Common.Settings;

public interface IFloatSliderGenericSetting : IGenericSetting
{
    float Min { get; }
    float Max { get; }
    float Value { get; set; }
    bool WholeNumbers { get; }
}