namespace WDE.EventAiEditor.Editor.UserControls
{
    public class DropActionsConditionsArgs
    {
        public int ActionIndex;
        public int EventIndex;
        public bool Copy;
        public bool Move => !Copy;
    }
}