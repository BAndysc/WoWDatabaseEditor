namespace WoWDatabaseEditor.Extensions
{
    public class ColumnDescriptor
    {
        public ColumnDescriptor(string headerText, string displayMember, float? preferredWidth = null, bool checkboxMember = false)
        {
            HeaderText = headerText;
            DisplayMember = displayMember;
            CheckboxMember = checkboxMember;
            PreferredWidth = preferredWidth;
        }

        public string HeaderText { get; set; }
        public string DisplayMember { get; set; }
        public bool CheckboxMember { get; set; }
        public float? PreferredWidth { get; }
    }
}