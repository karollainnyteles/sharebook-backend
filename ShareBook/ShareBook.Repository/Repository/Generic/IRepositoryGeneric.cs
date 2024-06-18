using ShareBook.Domain.Common;
using ShareBook.Repository.Repository;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ShareBook.Repository
{
    public interface IRepositoryGeneric<TEntity> : IGenericOperations<TEntity> where TEntity : class
    {
        Task<TEntity> FindAsync(params object[] keyValues);

        Task<TEntity> FindAsync(IncludeList<TEntity> includes, params object[] keyValues);

        Task<TEntity> FindAsync(IncludeList<TEntity> includes, Expression<Func<TEntity, bool>> filter);

        Task<PagedList<TEntity>> GetAsync<TKey>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TKey>> order, int page, int itemsPerPage);

        Task<PagedList<TEntity>> GetAsync<TKey>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TKey>> order, int page, int itemsPerPage, IncludeList<TEntity> includes);

        Task<int> CountAsync(Expression<Func<TEntity, bool>> filter);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter);

        Task<TEntity> InsertAsync(TEntity entity);

        Task<TEntity> UpdateAsync(TEntity entity);

        Task DeleteAsync(params object[] keyValues);

        Task DeleteAsync(TEntity entity);

        /// <summary>
        /// Get the DbSet as a IQueryable
        /// </summary>
        IQueryable<TEntity> Get();

        TEntity Update(TEntity entity);

        void Delete(params object[] keyValues);

        TEntity Insert(TEntity entity);

        IQueryable<TEntity> FromSql(string query, object[] parameters);
    }
}