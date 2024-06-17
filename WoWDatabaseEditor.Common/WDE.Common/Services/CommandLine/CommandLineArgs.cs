using System;
using System.Collections;
using System.Collections.Generic;

namespace WDE.Common.Services.CommandLine
{
    public class CommandLineArgs : ICommandLineArgs
    {
        private HashSet<string> setArgs = new();
        private Dictionary<string, string> keyValueArgs = new();
        private string[] arguments = Array.Empty<string>();

        public bool IsArgumentSet(string argument) => setArgs.Contains(argument.ToLower());

        public string? GetValue(string argument)
        {
            if (keyValueArgs.TryGetValue(argument, out var value))
                return value;
            return null;
        }
        
        public void Init(string[] args)
        {
            arguments = args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i].StartsWith("--"))
                {
                    var key = args[i].Substring(2).ToLower();
                    setArgs.Add(key);
                    if (i < args.Length - 1 && !args[i + 1].StartsWith("--"))
                    {
                        keyValueArgs[key] = args[i + 1];
                        i++;
                    }
                }
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>) arguments).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => arguments.Length;

        public string this[int index] => arguments[index];
    }
}