using System;

namespace WDE.SqlWorkbench.Services.TableUtils;

internal class NoPrimaryKeyColumnException : Exception
{
    public NoPrimaryKeyColumnException(string table) : base($"Table {table} has no primary key")
    {
    }
}