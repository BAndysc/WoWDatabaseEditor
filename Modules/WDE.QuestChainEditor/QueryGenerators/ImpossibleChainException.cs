using System;
using WDE.Common.Exceptions;

namespace WDE.QuestChainEditor.QueryGenerators;

public class ImpossibleChainException : UserException
{
    public ImpossibleChainException(string message) : base("This chain is impossible to generate: " + message)
    {
    }
}