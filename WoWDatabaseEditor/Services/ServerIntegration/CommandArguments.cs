using System;
using WDE.Common.Services;

namespace WoWDatabaseEditorCore.Services.ServerIntegration
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
    }
}