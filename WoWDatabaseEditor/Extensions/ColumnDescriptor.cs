namespace WoWDatabaseEditor.Extensions
{
    public class ColumnDescriptor
    {
        public ColumnDescriptor(string headerText, string displayMember, bool checkboxMember = false)
        {
            HeaderText = headerText;
            DisplayMember = displayMember;
            CheckboxMember = checkboxMember;
        }

        public string HeaderText { get; set; }
        public string DisplayMember { get; set; }
        public bool CheckboxMember { get; set; }
    }
}