namespace WoWDatabaseEditorCore.Extensions
{
    public class ColumnDescriptor
    {
        public ColumnDescriptor(string headerText, string displayMember, float? preferredWidth = null, bool checkboxMember = false, bool oneTime = true)
        {
            HeaderText = headerText;
            DisplayMember = displayMember;
            CheckboxMember = checkboxMember;
            PreferredWidth = preferredWidth;
            OneTime = oneTime;
        }

        public string HeaderText { get; set; }
        public string DisplayMember { get; set; }
        public bool CheckboxMember { get; set; }
        public float? PreferredWidth { get; }
        public bool OneTime { get; }
    }
}