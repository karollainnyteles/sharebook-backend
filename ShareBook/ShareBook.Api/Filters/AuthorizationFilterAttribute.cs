using Microsoft.AspNetCore.Mvc.Filters;
using ShareBook.Domain.Exceptions;
using ShareBook.Service.Authorization;
using System;
using System.Linq;
using System.Security.Claims;

namespace ShareBook.Api.Filters
{
    public class AuthorizationFilterAttribute : ActionFilterAttribute
    {
        public Permissions.Permission[] NecessaryPermissions { get; set; }

        public AuthorizationFilterAttribute(params Permissions.Permission[] permissions)
        {
            NecessaryPermissions = permissions;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;

            if (user == null)
                throw new ShareBookException(ShareBookException.Error.NotAuthorized);

            var isAdministrator = ((ClaimsIdentity)user.Identity).Claims
                .Any(x => x.Type == ClaimsIdentity.DefaultRoleClaimType.ToString() && x.Value == Domain.Enums.Profile.Administrator.ToString());

            if (Array.Exists(NecessaryPermissions, x => Permissions.AdminPermissions.Contains(x)) && !isAdministrator)
                throw new ShareBookException(ShareBookException.Error.Forbidden);

            base.OnActionExecuting(context);
        }
    }
}