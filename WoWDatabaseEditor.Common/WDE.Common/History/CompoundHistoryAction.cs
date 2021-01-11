namespace WDE.Common.History
{
    public class CompoundHistoryAction : IHistoryAction
    {
        private readonly string _name;
        private readonly IHistoryAction[] _subactions;

        public CompoundHistoryAction(string name, params IHistoryAction[] subactions)
        {
            _name = name;
            _subactions = subactions;
        }
        
        public void Undo()
        {
            for (int i = _subactions.Length - 1; i >= 0; --i)
                _subactions[i].Undo();
        }

        public void Redo()
        {
            for (int i = 0; i < _subactions.Length; ++i)
                _subactions[i].Redo();
        }

        public string GetDescription()
        {
            return _name;
        }
    }
}