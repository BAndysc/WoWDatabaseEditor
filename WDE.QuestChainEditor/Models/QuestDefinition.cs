namespace WDE.QuestChainEditor.Models
{
    public class QuestDefinition
    {
        public QuestDefinition(uint id, string title)
        {
            Id = id;
            Title = title;
        }

        public uint Id { get; }
        public string Title { get; }
    }
}