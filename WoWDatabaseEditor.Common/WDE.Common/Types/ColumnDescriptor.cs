namespace WDE.Common.Types
{
    public class ColumnDescriptor
    {
        private ColumnDescriptor(string headerText, string displayMember, float? preferredWidth = null, bool checkboxMember = false, bool oneTime = true,  object? dataTemplate = null)
        {
            DataTemplate = dataTemplate;
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
        
        public static ColumnDescriptor DataTemplateColumn(string headerText, object dataTemplate, float? preferredWidth = null, bool oneTime = true)
        {
            return new ColumnDescriptor(headerText, "", preferredWidth, false, oneTime, dataTemplate);
        }
        
        public string HeaderText { get; set; }
        public string DisplayMember { get; set; }
        public bool CheckboxMember { get; set; }
        public float? PreferredWidth { get; }
        public bool OneTime { get; }
        public object? DataTemplate { get; set; }
    }
}