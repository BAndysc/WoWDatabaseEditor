namespace WDE.Common.Database
{
    public interface ISmartScriptProjectItem
    {
        uint Id { get; set; }
        uint ProjectId { get; set; }
        byte Type { get; set; }
        int Value { get; set; }
        int? Value2 { get; set; }
        string? StringValue { get; set; }
        string? Comment { get; set; }
    }

    public class AbstractSmartScriptProjectItem : ISmartScriptProjectItem
    {
        public uint Id { get; set; }
        public uint ProjectId { get; set; }
        public byte Type { get; set; }
        public int Value { get; set; }
        public int? Value2 { get; set; }
        public string? StringValue { get; set; }
        public string? Comment { get; set; }

        public AbstractSmartScriptProjectItem() {}
        
        public AbstractSmartScriptProjectItem(ISmartScriptProjectItem item)
        {
            Id = item.Id;
            ProjectId = item.ProjectId;
            Type = item.Type;
            Value = item.Value;
            Value2 = item.Value2;
            StringValue = item.StringValue;
            Comment = item.Comment;
        }
    }
}