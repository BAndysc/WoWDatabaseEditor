using System;

namespace WDE.LootEditor.QueryGenerator;

public class LootDuplicateKeysException : Exception
{
    public LootDuplicateKeysException(string message) : base(message) { }
}