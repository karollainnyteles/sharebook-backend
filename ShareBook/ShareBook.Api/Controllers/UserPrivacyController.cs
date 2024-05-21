using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ShareBook.Api.Filters;
using ShareBook.Domain.Common;
using ShareBook.Domain.DTOs;
using ShareBook.Domain.Exceptions;
using ShareBook.Service.Lgpd;
using System;
using System.Threading;

namespace ShareBook.Api.Controllers;

[Route("api/[controller]")]
[EnableCors("AllowAllHeaders")]
[GetClaimsFilter]
public class UserPrivacyController : ControllerBase
{
    private readonly ILgpdService _lgpdService;

    public UserPrivacyController(ILgpdService lgpdService)
    {
        _lgpdService = lgpdService;
    }

    [HttpPost("Anonymize")]
    [Authorize("Bearer")]
    public IActionResult Anonymize([FromBody] UserAnonymizeDTO dto)
    {
        var userIdFromSession = new Guid(Thread.CurrentPrincipal?.Identity?.Name);
        if (dto.UserId != userIdFromSession)
            throw new ShareBookException(ShareBookException.Error.Forbidden,
                "Você não tem permissão para remover esse conta.");

        _lgpdService.Anonymize(dto);
        return Ok(new Result("Sua conta foi removida com sucesso."));
    }
}