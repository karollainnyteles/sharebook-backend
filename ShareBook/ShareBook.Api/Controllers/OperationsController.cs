using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Sharebook.Jobs;
using ShareBook.Api.Filters;
using ShareBook.Api.ViewModels;
using ShareBook.Helper.Extensions;
using ShareBook.Service;
using ShareBook.Service.Authorization;
using ShareBook.Service.Server;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ShareBook.Api.Controllers;

[Route("api/[controller]")]
[EnableCors("AllowAllHeaders")]
public class OperationsController : Controller
{
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;
    private readonly IWebHostEnvironment _env;
    protected IJobExecutor JobExecutor;
    protected string ValidToken;

    public OperationsController(IJobExecutor jobExecutor, IOptions<ServerSettings> settings, IEmailService emailService,
        IWebHostEnvironment env, IConfiguration configuration, IMemoryCache memoryCache)
    {
        JobExecutor = jobExecutor;
        ValidToken = settings.Value.JobExecutorToken;
        _emailService = emailService;
        _env = env;
        _cache = memoryCache;
    }

    [HttpGet("Ping")]
    public IActionResult Ping()
    {
        var ass = Assembly.GetEntryAssembly();
        var result = new
        {
            Service = ass.GetName().Name,
            Version = ass.GetName().Version.ToString(),
            DotNetVersion = Environment.Version.ToString(),
            BuildLinkerTime = ass.GetLinkerTime().ToString("dd/MM/yyyy HH:mm:ss:fff z"),
            Env = _env.EnvironmentName,
            TimeZone = TimeZoneInfo.Local.DisplayName,
            RuntimeInformation.OSDescription,
            MemoryCacheCount = ((MemoryCache)_cache).Count
        };
        return Ok(result);
    }

    [HttpGet("JobExecutor")]
    [Throttle(Name = "JobExecutor", Seconds = 5, VaryByIp = false)]
    public IActionResult Executor()
    {
        if (!IsValidJobToken())
            return Unauthorized();
        return Ok(JobExecutor.Execute());
    }

    [HttpPost("EmailTest")]
    [Authorize("Bearer")]
    [AuthorizationFilter(Permissions.Permission.ApproveBook)] // adm
    public IActionResult EmailTest([FromBody] EmailTestVM emailVM)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _emailService.Test(emailVM.Email, emailVM.Name).Wait();
        return Ok();
    }

    protected bool IsValidJobToken()
    {
        return Request.Headers[HeaderNames.Authorization].ToString() == ValidToken;
    }
}