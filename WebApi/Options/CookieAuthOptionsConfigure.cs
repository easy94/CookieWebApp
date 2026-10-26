using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace WebApi.Options;

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
        options.AccessDeniedPath = new PathString(_config.AccessDeniedPath ?? "/");
        options.ExpireTimeSpan = _config.ExpireTimeSpan;
        options.SlidingExpiration = _config.SlidingExpiration;
        options.Cookie.SecurePolicy = _config.CookieSecurePolicy
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.Cookie.HttpOnly = _config.CookieHttpOnly;

        /*not important right now
        options.Events.OnValidatePrincipal = async func =>
        {
            var db = func.HttpContext.RequestServices.GetRequiredService<UserContainerDb>();
            db.Users.Find()
            func.Principal.HasClaim()
            func.RejectPrincipal();
            
            return;
        };
        */
    }
}