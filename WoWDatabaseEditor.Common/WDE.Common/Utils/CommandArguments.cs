using System;
using System.Linq;
using WDE.Common.Services;

namespace WDE.Common.Utils
{
    public class CommandArguments : ICommandArguments
    {
        private string[] args;
        private int iterator = 0;
        
        public CommandArguments(string args)
        {
            args = args.Trim();
            if (string.IsNullOrEmpty(args))
                this.args = Array.Empty<string>();
            else
                this.args = args.Split(" ");
        }

        public int LeftArguments => TotalArguments - iterator;
        
        public int TotalArguments => args.Length;
        public string TakeRestArguments
        {
            get
            {
                var result = string.Join(" ", args.Skip(iterator));
                iterator = TotalArguments;
                return result;
            }
        }

        public bool TryGetString(out string word)
        {
            word = null!;
            if (LeftArguments == 0)
                return false;

            word = args[iterator++];
            
            return true;
        }
        
        public bool TryGetUint(out uint number)
        {
            number = 0;
            if (!TryGetString(out var word))
                return false;

            return uint.TryParse(word, out number);
        }
        
        public bool TryGetInt(out int number)
        {
            number = 0;
            if (!TryGetString(out var word))
                return false;

            return int.TryParse(word, out number);
        }
        
        public bool TryGetFloat(out float number)
        {
            number = 0;
            if (!TryGetString(out var word))
                return false;

            return float.TryParse(word, out number);
        }
    }
}