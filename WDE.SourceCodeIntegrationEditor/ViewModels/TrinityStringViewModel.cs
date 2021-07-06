namespace WDE.SourceCodeIntegrationEditor.ViewModels
{
    public class TrinityStringViewModel
    {
        public TrinityStringViewModel(uint id, string enumName)
        {
            Id = id;
            EnumName = enumName;
        }

        public uint Id { get; }
        public string EnumName { get; } = "";
        public bool IsSelected { get; set; }
        public string ContentDefault { get; set; } = "";
    }
}