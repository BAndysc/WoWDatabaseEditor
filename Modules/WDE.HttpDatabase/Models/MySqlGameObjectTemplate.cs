
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonGameObjectTemplate : IGameObjectTemplate
    {
        
        public uint Entry { get; set; }

        
        public uint DisplayId { get; set; }

        
        public float Size { get; set; }

        
        public GameobjectType Type { get; set; }

        public uint Flags => 0;

        
        public string Name { get; set; } = "";

         
        public string AIName { get; set; } = "";

         
        public string ScriptName { get; set; } = "";

        
        public int Data0 { get; set; }

        
        public int Data1 { get; set; }

        public uint this[int dataIndex]
        {
            get
            {
                if (dataIndex == 0)
                    return Data0 < 0 ? 0 : (uint)Data0;
                if (dataIndex == 1)
                    return Data1 < 0 ? 0 : (uint)Data1;
                return 0;
            }
        }
        
        public uint DataCount => 2;
    }
}