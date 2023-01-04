
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonGossipMenuLine
    {
        
        public uint MenuId { get; set; }

        
        public uint TextId { get; set; }
        
        public INpcText? Text { get; set; }

        internal JsonGossipMenuLine SetText(INpcText text)
        {
            Text = text;
            return this;
        }
    }
}