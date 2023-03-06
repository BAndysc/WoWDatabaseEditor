namespace WDE.Common.Types
{
    public class ColumnDescriptor
    {
        private ColumnDescriptor(string headerText, string displayMember, float? preferredWidth = null, bool checkboxMember = false, bool oneTime = true)
        {
            HeaderText = headerText;
            DisplayMember = displayMember;
            CheckboxMember = checkboxMember;
            PreferredWidth = preferredWidth;
            OneTime = oneTime;
        }
        
        public static ColumnDescriptor CheckBoxColumn(string headerText, string displayMember, float? preferredWidth = null, bool oneTime = true)
        {
            return new ColumnDescriptor(headerText, displayMember, preferredWidth, true, oneTime);
        }
        
        public static ColumnDescriptor TextColumn(string headerText, string displayMember, float? preferredWidth = null, bool oneTime = true)
        {
            return new ColumnDescriptor(headerText, displayMember, preferredWidth, false, oneTime);
        }

        public string HeaderText { get; set; }
        public string DisplayMember { get; set; }
        public bool CheckboxMember { get; set; }
        public float? PreferredWidth { get; }
        public bool OneTime { get; }
    }
}