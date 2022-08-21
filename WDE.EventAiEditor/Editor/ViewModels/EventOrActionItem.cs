using System;
using WDE.MVVM;

namespace WDE.EventAiEditor.Editor.ViewModels
{
    public class EventOrActionItem : ObservableBase
    {
        private readonly Action<EventOrActionItem, bool>? makeFavourite;
        private int score = 100;
        private readonly string searchName = "";
        private bool isFavourite;

        public EventOrActionItem(string group, bool isFavourite, Action<EventOrActionItem, bool>? makeFavourite)
        {
            this.makeFavourite = makeFavourite;
            Group = originalGroup = group;
            IsFavourite = isFavourite;
        }
        
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

        public bool IsFavourite
        {
            get => isFavourite;
            set
            {
                if (makeFavourite == null)
                    return;
                Group = value ? "Favourites" : originalGroup;
                RaisePropertyChanged(nameof(Group));
                SetProperty(ref isFavourite, value);
                makeFavourite(this, value);
            }
        }

        public string EnumName { get; init; } = "";

        public string Name { get; init; } = "";

        public string SearchName
        {
            get => searchName;
            init => searchName = value.ToLower();
        }

        public bool Deprecated { get; init; }

        public string Help { get; init; } = "";

        public uint Id  { get; init; }
        public uint? CustomId  { get; init; }

        private string originalGroup;
        public string Group { get; private set; }

        public bool IsTimed { get; init; }

        public int Order  { get; init; }
    }
}