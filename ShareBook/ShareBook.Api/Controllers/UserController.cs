using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ShareBook.Api.Filters;
using ShareBook.Api.ViewModels;
using ShareBook.Domain;
using ShareBook.Domain.Common;
using ShareBook.Domain.DTOs;
using ShareBook.Domain.Exceptions;
using ShareBook.Helper;
using ShareBook.Infra.CrossCutting.Identity;
using ShareBook.Infra.CrossCutting.Identity.Interfaces;
using ShareBook.Service;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ShareBook.Api.Controllers;

[Route("api/[controller]")]
[EnableCors("AllowAllHeaders")]
[GetClaimsFilter]
public class UserController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly IApplicationSignInManager _signManager;
    private readonly IUserService _userService;

    public UserController(IUserService userService, IApplicationSignInManager signManager, IMapper mapper,
        IConfiguration configuration)
    {
        _userService = userService;
        _signManager = signManager;
        _mapper = mapper;
        _configuration = configuration;
    }

    [HttpGet]
    [Authorize("Bearer")]
    public UserVM
        Get()
    {
        var id = new Guid(Thread.CurrentPrincipal?.Identity?.Name);
        var user = _userService.Find(id);
        var userVm = _mapper.Map<UserVM>(user);
        return userVm;
    }

    [Authorize("Bearer")]
    [HttpGet("Profile")]
    public object Profile()
    {
        var id = new Guid(Thread.CurrentPrincipal?.Identity?.Name);
        return new { profile = _userService.Find(id).Profile.ToString() };
    }

    [Authorize("Bearer")]
    [HttpGet("ListFacilitators/{userIdDonator}")]
    public IActionResult ListFacilitators(Guid userIdDonator)
    {
        var facilitators = _userService.GetFacilitators(userIdDonator);

        var facilitatorsClean = _mapper.Map<List<UserFacilitatorVM>>(facilitators);

        return Ok(facilitatorsClean);
    }

    [HttpPost("Register")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(409)]
    public IActionResult Post([FromBody] RegisterUserDTO registerUserDto,
        [FromServices] SigningConfigurations signingConfigurations,
        [FromServices] TokenConfigurations tokenConfigurations)
    {
        var result = _userService.Insert(registerUserDto);

        if (result.Success)
        {
            if (registerUserDto.Age > 12)
                return Ok(_signManager.GenerateTokenAndSetIdentity(result.Value, signingConfigurations,
                    tokenConfigurations));
            return Ok(new Result(
                "Seu cadastro foi realizado com sucesso. Foi enviado um email para os pais solicitando o consentimento. Vamos te avisar por email quando seu acesso for liberado. Obrigado. =)"));
        }

        return Conflict(result);
    }

    [HttpPost("Login")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public IActionResult Login(
        [FromBody] LoginUserVM loginUserVM,
        [FromServices] SigningConfigurations signingConfigurations,
        [FromServices] TokenConfigurations tokenConfigurations,
        [FromHeader(Name = "x-requested-with")]
        string client,
        [FromHeader(Name = "client-version")] string clientVersion)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // mensagem amigável para usuários mobile antigos
        if (!IsValidClientVersion(client, clientVersion))
            throw new ShareBookException(
                "Não é possível fazer login porque seu app está desatualizado. Por favor atualize seu app na loja do Google Play.");

        var user = _mapper.Map<User>(loginUserVM);
        var result = _userService.AuthenticationByEmailAndPassword(user);

        if (result.Success)
        {
            var response = new Result
            {
                Value = _signManager.GenerateTokenAndSetIdentity(result.Value, signingConfigurations,
                    tokenConfigurations)
            };

            return Ok(response);
        }

        return NotFound(result);
    }

    [HttpPost("ForgotMyPassword")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(404)]
    public IActionResult ForgotMyPassword([FromBody] ForgotMyPasswordVM forgotMyPasswordVm)
    {
        var result = _userService.GenerateHashCodePasswordAndSendEmailToUser(forgotMyPasswordVm.Email);

        if (result.Success)
            return Ok(result);

        return NotFound(result);
    }

    [HttpPut]
    [Authorize("Bearer")]
    [ProducesResponseType(typeof(Result<User>), 200)]
    [ProducesResponseType(409)]
    public IActionResult Update([FromBody] UpdateUserVM updateUserVm,
        [FromServices] SigningConfigurations signingConfigurations,
        [FromServices] TokenConfigurations tokenConfigurations)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = _mapper.Map<User>(updateUserVm);

        user.Id = new Guid(Thread.CurrentPrincipal?.Identity?.Name);

        var result = _userService.Update(user);

        if (!result.Success)
            return Conflict(result);

        return Ok(_signManager.GenerateTokenAndSetIdentity(result.Value, signingConfigurations, tokenConfigurations));
    }

    [Authorize("Bearer")]
    [HttpPut("ChangePassword")]
    public Result<User> ChangePassword([FromBody] ChangePasswordUserVM changePasswordUserVm)
    {
        var user = new User { Password = changePasswordUserVm.OldPassword };
        user.Id = new Guid(Thread.CurrentPrincipal?.Identity?.Name);
        return _userService.ValidOldPasswordAndChangeUserPassword(user, changePasswordUserVm.NewPassword);
    }

    [HttpPut("ChangeUserPasswordByHashCode")]
    [ProducesResponseType(typeof(Result<User>), 200)]
    [ProducesResponseType(404)]
    public IActionResult ChangeUserPasswordByHashCode(
        [FromBody] ChangeUserPasswordByHashCodeVM changeUserPasswordByHashCodeVm)
    {
        var result = _userService.ConfirmHashCodePassword(changeUserPasswordByHashCodeVm.HashCodePassword);
        if (!result.Success)
            return NotFound(result);
        var newPassword = changeUserPasswordByHashCodeVm.NewPassword;
        var user = _userService.Find((result.Value as User).Id);
        user.Password = newPassword;

        var resultChangePasswordUser = _userService.ChangeUserPassword(user, newPassword);

        if (!resultChangePasswordUser.Success)
            return BadRequest(resultChangePasswordUser);

        return Ok(resultChangePasswordUser);
    }

    [HttpPut("ParentAproval")]
    public IActionResult ParentAproval([FromBody] ParentAprovalVM parentAprovalVm)
    {
        var ParentHashCodeAproval = parentAprovalVm.ParentHashCodeAproval;

        if (string.IsNullOrEmpty(ParentHashCodeAproval) || !Guid.TryParse(ParentHashCodeAproval, out _))
            throw new ShareBookException("Código inválido.");

        _userService.ParentAproval(ParentHashCodeAproval);
        return Ok();
    }

    private bool IsValidClientVersion(string client, string clientVersion)
    {
        switch (client)
        {
            case "web":
                return true;

            // mobile android
            case "com.makeztec.sharebook":
                var minVersion = _configuration["ClientSettings:AndroidMinVersion"];
                return ClientVersionValidation.IsValidVersion(clientVersion, minVersion);

            default:
                return false;
        }
    }
}