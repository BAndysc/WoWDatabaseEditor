using WDE.MVVM;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartItem : ObservableBase
    {
        private int score = 100;
        private string name = "";
        private string enumName = "";
        private bool deprecated;
        private bool isTimed;
        private string group = "";
        private string searchName = "";

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

        public string EnumName
        {
            get => enumName;
            set => SetProperty(ref enumName, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string SearchName
        {
            get => searchName;
            set => SetProperty(ref searchName, value.ToLower());
        }

        public bool Deprecated
        {
            get => deprecated;
            set => SetProperty(ref deprecated, value);
        }

        public string Help { get; set; } = "";
        public int Id { get; set; }
        public int? CustomId { get; set; }

        public string Group
        {
            get => group;
            set => SetProperty(ref group, value);
        }

        public bool IsTimed
        {
            get => isTimed;
            set => SetProperty(ref isTimed, value);
        }

        public int Order { get; set; }

        public SmartItem Update(SmartItem other)
        {
            Name = other.Name;
            IsTimed = other.IsTimed;
            Deprecated = other.Deprecated;
            Score = other.Score;
            Group = other.Group;
            Order = other.Order;
            Id = other.Id;
            CustomId = other.CustomId;
            Help = other.Help;
            EnumName = other.EnumName;
            SearchName = other.SearchName;
            return this;
        }
    }
}