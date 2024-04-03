﻿using ShareBook.Domain;
using ShareBook.Domain.Enums;
using System.Threading.Tasks;

namespace ShareBook.Service
{
    public interface IAccessHistoryService
    {
        Task InsertVisitor(User user, User visitor, VisitorProfile profile);
    }
}