using System;
using System.Collections.Generic;

namespace ShareBook.Domain.Exceptions;

public class ShareBookException : Exception
{
    public enum Error
    {
        BadRequest = 400,
        NotAuthorized = 401,
        Forbidden = 403,
        NotFound = 404
    }

    public static readonly Dictionary<Error, string> ErrorMessages = new()
    {
        { Error.NotAuthorized, "O usuário precisa estar logado para efetuar essa ação." },
        { Error.Forbidden, "Usuário não tem as permissões necessárias para efetuar esta ação." },
        { Error.NotFound, "Entidade não encontrada. Por favor, verifique." }
    };

    public ShareBookException(string message) : this(Error.BadRequest, message)
    {
    }

    public ShareBookException(Error error) : this(error, ErrorMessages[error])
    {
    }

    public ShareBookException(Error error, string message) : base(message)
    {
        ErrorType = error;
    }

    public Error ErrorType { get; set; }
}