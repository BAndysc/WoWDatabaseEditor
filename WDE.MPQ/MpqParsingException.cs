// Original code by https://github.com/Thenarden/nmpq 
// Apache-2.0 License
using System;

namespace WDE.MPQ
{
    public class MpqParsingException : Exception
    {
        public MpqParsingException(string message) : base(message)
        {
        }
    }
}