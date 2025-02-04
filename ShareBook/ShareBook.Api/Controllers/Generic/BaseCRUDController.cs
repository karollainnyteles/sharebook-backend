﻿using AutoMapper;
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
    public abstract class BaseCrudController<T> : BaseCrudController<T, T, T>
        where T : BaseEntity
    {
        protected BaseCrudController(IBaseService<T> service, IMapper mapper) : base(service, mapper)
        {
        }
    }

    public abstract class BaseCrudController<T, R> : BaseDeleteController<T, R, T>
       where T : BaseEntity
       where R : BaseViewModel
    {
        protected BaseCrudController(IBaseService<T> service) : base(service)
        {
        }
    }

    [GetClaimsFilter]
    [Route("api/[controller]")]
    [EnableCors("AllowAllHeaders")]
    public abstract class BaseCrudController<T, R, A> : BaseDeleteController<T, R, A>
        where T : BaseEntity
        where R : IIdProperty
        where A : class
    {
        protected readonly IMapper _mapper;

        protected BaseCrudController(IBaseService<T> service, IMapper mapper) : base(service)
        {
            _mapper = mapper;
        }

        [Authorize("Bearer")]
        [HttpPost]
        public virtual Result<A> Create([FromBody] R viewModel)
        {
            if (!HasRequestViewModel)
                return _mapper.Map<Result<A>>(_service.Insert(viewModel as T));

            var entity = _mapper.Map<T>(viewModel);
            var result = _service.Insert(entity);

            var resultVM = _mapper.Map<Result<A>>(result);
            return resultVM;
        }

        [Authorize("Bearer")]
        [HttpPut("{id}")]
        public virtual Result<A> Update(Guid id, [FromBody] R viewModel)
        {
            viewModel.Id = id;

            if (!HasRequestViewModel)
                return _mapper.Map<Result<A>>(_service.Update(viewModel as T));

            var entity = _mapper.Map<T>(viewModel);
            var result = _service.Update(entity);
            var resultVM = _mapper.Map<A>(result);
            return new Result<A>(resultVM);
        }
    }
}