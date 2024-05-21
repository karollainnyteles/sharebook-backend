using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShareBook.Api.Filters;
using ShareBook.Api.ViewModels;
using ShareBook.Domain;
using ShareBook.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareBook.Api.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("AllowAllHeaders")]
    [GetClaimsFilter]
    public class UserActivityController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IAccessHistoryRepository _historyRepository;

        public UserActivityController(IMapper mapper, IAccessHistoryRepository historyRepository)
        {
            _mapper = mapper;
            _historyRepository = historyRepository;
        }

        [Authorize("Bearer")]
        [HttpGet("WhoAccessed/{userId:Guid}")]
        [ProducesResponseType(typeof(AccessHistoryVM), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> WhoAccessedMyProfile(Guid userId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (userId.Equals(null) || userId.Equals(Guid.Empty)) return BadRequest(ModelState);

            var whoAccessHistory = _mapper.Map<IEnumerable<AccessHistory>, IEnumerable<AccessHistoryVM>>(
                await _historyRepository.GetWhoAccessedMyProfile(userId));

            if (whoAccessHistory is null) return NotFound(userId);

            return Ok(whoAccessHistory);
        }
    }
}