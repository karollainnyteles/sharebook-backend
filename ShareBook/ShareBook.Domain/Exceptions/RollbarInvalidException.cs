using System;
using System.Runtime.Serialization;

namespace ShareBook.Domain.Exceptions;

[Serializable]
public class RollbarInvalidException : Exception
{
    public RollbarInvalidException(string message) : base(message)
    {
    }

    protected RollbarInvalidException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}