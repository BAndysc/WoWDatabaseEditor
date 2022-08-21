namespace WDE.EventAiEditor.Validation
{
    public interface IEventAiValidator
    {
        bool Evaluate(IEventAiValidationContext context);
    }
}