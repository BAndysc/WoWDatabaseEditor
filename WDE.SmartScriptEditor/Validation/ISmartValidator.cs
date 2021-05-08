namespace WDE.SmartScriptEditor.Validation
{
    public interface ISmartValidator
    {
        bool Evaluate(ISmartValidationContext context);
    }
}