using WDE.MVVM;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartItem : ObservableBase
    {
        private int score = 100;

        public bool ShowItem => Score > 61;
        
        public int Score
        {
            get => score;
            set
            {
                SetProperty(ref score, value);
                RaisePropertyChanged(nameof(ShowItem));
            }
        }

        public string EnumName { get; init; } = "";

        public string Name { get; init; } = "";

        public string SearchName { get; init; } = "";

        public bool Deprecated { get; init; }

        public string Help { get; init; } = "";

        public int Id  { get; init; }
        public int? CustomId  { get; init; }

        public string Group { get; init; } = "";

        public bool IsTimed { get; init; }

        public int Order  { get; init; }
    }
}