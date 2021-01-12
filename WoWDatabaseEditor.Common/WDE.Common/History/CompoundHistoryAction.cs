namespace WDE.Common.History
{
    public class CompoundHistoryAction : IHistoryAction
    {
        private readonly string name;
        private readonly IHistoryAction[] subactions;

        public CompoundHistoryAction(string name, params IHistoryAction[] subactions)
        {
            this.name = name;
            this.subactions = subactions;
        }

        public void Undo()
        {
            for (int i = subactions.Length - 1; i >= 0; --i)
                subactions[i].Undo();
        }

        public void Redo()
        {
            for (var i = 0; i < subactions.Length; ++i)
                subactions[i].Redo();
        }

        public string GetDescription()
        {
            return name;
        }
    }
}