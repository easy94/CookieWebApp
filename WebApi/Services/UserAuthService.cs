using System;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;
using WebApi.Models;

namespace WebApi.Services;

public class UserAuthService(HttpContext httpContext, UserContainerDb db, PasswordHasher<User> hasher):IUserAuthService<ICookieMarker>
{
    public async Task<LoginResult> UserLoginAsync(LoginDTO login)
    {
        var user = await db.Users.FirstOrDefaultAsync(p => p.Email == login.Email);

        if(httpContext.User.Identity?.IsAuthenticated is true)
            return new LoginResult (false, "already logged in", user);

        if (user is null) 
            return new LoginResult (false, "email doesnt exist", null);

        var pw = hasher.HashPassword(user, login.Password);

        if(hasher.VerifyHashedPassword(user, user.PasswordHash, login.Password) == PasswordVerificationResult.Success)       
            return new LoginResult(false, "bad password", user);

        //after verifying pw,email create sessionid for authentication OnValidate hook 
        //to check if cookie has been revoked (cache needed for that to reduce db strain)
        //var sid = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, "user"),
            //new(ClaimTypes.Sid, sid.ToString()),
            new("verified", "false")
        };

        AuthenticationProperties authproperties = new() { IssuedUtc = DateTime.UtcNow };
        ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authproperties);

        //user.Sid = sid;
        await db.SaveChangesAsync();

        return new LoginResult(true, null, user);
    }
}