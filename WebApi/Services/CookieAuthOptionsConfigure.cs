using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApi.Options;
namespace WebApi.Services;

class CookieAuthOptionsConfigure : IConfigureOptions<CookieAuthenticationOptions>
{
    public CookieAuthOptions _config;

    public CookieAuthOptionsConfigure(IOptions<CookieAuthOptions> config)
    {
        _config = config.Value;
    }

    public void Configure(CookieAuthenticationOptions options)
    {
        options.LoginPath = _config.LoginPath;
        options.LogoutPath = _config.LogoutPath;
        options.AccessDeniedPath = _config.AccessDeniedPath;
        options.ExpireTimeSpan = _config.ExpireTimeSpan;
        options.SlidingExpiration = _config.SlidingExpiration;
        options.Cookie.HttpOnly = _config.CookieHttpOnly;

        options.Events.OnValidatePrincipal = async func =>
        {
            var db = func.HttpContext.RequestServices.GetRequiredService<UserContainerDb>();
            var user = await db.Users.FirstOrDefaultAsync(p => p.Email == func.HttpContext.User.FindFirstValue(ClaimTypes.Email));
            if (func.Principal?.FindFirstValue(ClaimTypes.Sid) != user?.Sid.ToString()){
                func.RejectPrincipal();
                func.ShouldRenew = true;
            }
            return;
        };
        
    }
}