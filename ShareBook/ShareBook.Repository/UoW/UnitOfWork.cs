﻿using System;

namespace ShareBook.Repository.UoW
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context) => _context = context;

        public void BeginTransaction() => _context.Database.BeginTransaction();

        public void Commit() => _context.Database.CommitTransaction();

        public void Rollback() => _context.Database.RollbackTransaction();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Database.CurrentTransaction?.Rollback();
            }
        }
    }
}