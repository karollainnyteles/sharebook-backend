using System;
using System.Runtime.Serialization;

namespace ShareBook.Helper.Exceptions;

[Serializable]
public class FormatVersionInvalidException : Exception
{
    public FormatVersionInvalidException(string message) : base(message)
    {
    }

    protected FormatVersionInvalidException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}