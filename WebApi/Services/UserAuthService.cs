using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;
using WebApi.Models;

namespace WebApi.Services;

public class UserAuthService(UserContainerDb db, PasswordHasher<User> hasher, IHttpContextAccessor httpContext, ILogger<UserAuthService> logger):IUserAuthService<ICookieMarker>
{
    public async Task<LoginResult> UserLoginAsync(LoginDTO login)
    {
        var user = await db.Users.FirstOrDefaultAsync(p => p.Email == login.Email);

        if(httpContext.HttpContext?.User.Identity?.IsAuthenticated is true)
            return new LoginResult (false, "already logged in", user);

        if (user is null) 
            return new LoginResult (false, "email doesnt exist", null);

        var pw = hasher.HashPassword(user, login.Password);

        if (hasher.VerifyHashedPassword(user, user.PasswordHash, login.Password) != PasswordVerificationResult.Success)
            return new LoginResult(false, "bad password", user);

        //after verifying pw,email create sessionid for authentication OnValidate hook 
        //to check if cookie has been revoked (cache needed for that to reduce db strain)
        //var sid = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            //new(ClaimTypes.Sid, sid.ToString()),
            new("verified", "false")
        };

        AuthenticationProperties authproperties = new() { IssuedUtc = DateTime.UtcNow };
        ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);

        var currentHttpContext = httpContext.HttpContext;
        if (currentHttpContext is null)
            return new LoginResult(false, "http context unavailable", user);

        await currentHttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authproperties);

        //user.Sid = sid;
        await db.SaveChangesAsync();
        httpContext.HttpContext?.User.Claims.ToList().ForEach(
            p => logger.Log(LogLevel.Information, p.ValueType.ToString() + " " + p.Value.ToString()));
        return new LoginResult(true, null, user);
    }
}