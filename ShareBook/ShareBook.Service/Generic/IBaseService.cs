using ShareBook.Domain.Common;
using ShareBook.Repository;

namespace ShareBook.Service.Generic
{
    public interface IBaseService<TEntity> : IGenericOperations<TEntity> where TEntity : class
    {
        Result<TEntity> Update(TEntity entity);

        Result<TEntity> Insert(TEntity entity);

        Result Delete(params object[] keyValues);
    }
}