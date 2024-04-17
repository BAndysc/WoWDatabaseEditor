using System;

namespace WDE.Common.Exceptions;

/// <summary>
/// an exception that is not critical, it shall be displayed to the user
/// but there is no need to log it or anything, just an error for the user
/// </summary>
public class UserException : Exception
{
    public string? Header { get; }

    public UserException(string header, string message) : base(message)
    {
        Header = header;
    }

    public UserException(Exception inner) : base(inner.Message, inner)
    {
    }

    public UserException(string message) : base(message)
    {
    }
}