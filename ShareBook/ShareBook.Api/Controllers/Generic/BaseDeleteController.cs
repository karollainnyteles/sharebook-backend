using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ShareBook.Api.Filters;
using ShareBook.Api.ViewModels;
using ShareBook.Domain.Common;
using ShareBook.Service.Generic;
using System;

namespace ShareBook.Api.Controllers
{
    public abstract class BaseDeleteController<T> : BaseDeleteController<T, T, T>
        where T : BaseEntity
    {
        protected BaseDeleteController(IBaseService<T> service) : base(service)
        {
        }
    }

    public abstract class BaseDeleteController<T, R> : BaseDeleteController<T, R, T>
       where T : BaseEntity
       where R : BaseViewModel
    {
        protected BaseDeleteController(IBaseService<T> service) : base(service)
        {
        }
    }

    [GetClaimsFilter]
    [EnableCors("AllowAllHeaders")]
    [Route("api/[controller]")]
    public abstract class BaseDeleteController<T, R, A> : BaseController<T, R, A>
        where T : BaseEntity
        where R : IIdProperty
        where A : class
    {
        protected BaseDeleteController(IBaseService<T> service) : base(service)
        {
        }

        [Authorize("Bearer")]
        [HttpDelete("{id}")]
        public Result Delete(Guid id) => _service.Delete(id);
    }
}