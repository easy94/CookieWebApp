using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebApi.Services;
using WebApi.Models;
using Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using WebApi.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionstring = builder.Configuration.GetConnectionString("UserContainerDb") ?? "Data Source=../usercontainer.db";
builder.Services.AddSqlite<UserContainerDb>(connectionstring);
builder.Services.Configure<CookieAuthOptions>(builder.Configuration.GetSection(nameof(CookieAuthOptions)));
builder.Services.ConfigureOptions<CookieAuthOptionsConfigure>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserAuthService<ICookieMarker>, UserAuthService>();
builder.Services.AddLogging();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<PasswordHasher<User>>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(p => {

        var options = builder.Configuration.GetRequiredSection(nameof(CookieAuthOptions))
                    .Get<CookieAuthOptions>() ?? throw new OptionsValidationException(nameof(CookieAuthOptions), typeof(CookieAuthenticationOptions), ["CookieAuthenticationOptions configuration null Reference"]);
        p.AccessDeniedPath = options.AccessDeniedPath;
        p.LoginPath = options.LoginPath;
        p.LogoutPath = options.LogoutPath;
        p.ExpireTimeSpan = options.ExpireTimeSpan;
        p.Cookie.HttpOnly = true;
        p.SlidingExpiration = true;
        p.Events.OnValidatePrincipal = async func =>{

        var db = func.HttpContext.RequestServices.GetRequiredService<UserContainerDb>();
            var user = await db.Users.FirstOrDefaultAsync(p => p.Email == func.HttpContext.User.FindFirstValue(ClaimTypes.Email));
            if (func.Principal?.FindFirstValue(ClaimTypes.Sid) != user?.Sid.ToString()){
                func.RejectPrincipal();
                func.ShouldRenew = true;
            }
            return;
        };
    });

builder.Services
    .AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build();

        options.AddPolicy("AdminPolicy", new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireRole("admin")
            .Build());
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    var sp = app.Services.CreateScope();
    TestSeedDataBase.SeedDatabase(sp.ServiceProvider);
}

app.UseAuthentication();
app.UseAuthorization();

//Endpoints are resolved for each request scope

app.MapPost("/register", async ([FromForm] LoginDTO login, UserContainerDb db, PasswordHasher<User> pwHasher) =>
{
    //check if supplied email exists already
    if (await db.Users.AnyAsync(u => u.Email == login.Email)) return Results.BadRequest("User with this email already exists");

    var newuser = new User
    {
        Role = "user",
        Email = login.Email
    };
    newuser.PasswordHash = pwHasher.HashPassword(newuser, login.Password);

    var u = await db.Users.AddAsync(newuser);
    await db.SaveChangesAsync();

    return Results.LocalRedirect("/home.html", true, false);

}).AllowAnonymous().DisableAntiforgery();

app.MapPost("/login", async ([FromForm] LoginDTO login, IUserAuthService<ICookieMarker> auth, ILogger<User> logger) =>
{
    LoginResult loginResult = await auth.UserLoginAsync(login);
    if (!loginResult.Success)
    {
        return Results.BadRequest($"User: {loginResult.User}: {loginResult.Error}");
    }
    if (loginResult.User?.Role == "admin")
    {
        logger.Log(LogLevel.Information, loginResult.User.Email + " " + "admin authorized");
        return Results.Redirect("/adminsz");
    }

    logger.Log(LogLevel.Information, loginResult.User?.Email + " " + "user authorized");
    return Results.Redirect($"/homez");

}).AllowAnonymous().DisableAntiforgery();

app.MapPost("/logout", async (HttpContext httpContext, UserContainerDb db, ILogger<User> logger) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    httpContext.User.Identities.ToList().ForEach(p=> logger.Log(LogLevel.Information, p.IsAuthenticated.ToString()));
    var user = await db.Users.FirstOrDefaultAsync(p => p.Email == httpContext.User.FindFirstValue(ClaimTypes.Email));
    user!.Sid = Guid.NewGuid();
    await db.SaveChangesAsync();
    return Results.File("login.html", "text/html");
}).DisableAntiforgery();

app.MapPost("/container", async (string s, UserContainerDb db) =>
{
    var container = new Container
    {
        Name = s,
        Status = StatusEnum.NotRunning
    };
    var ct = await db.Containers.AddAsync(container);
    await db.SaveChangesAsync();
    return Results.Created($"/container/{ct.Entity.Id}", s);
});

app.MapPost("/container/{i}/start", async (int i, UserContainerDb db) =>
{
    var c = await db.Containers.FindAsync(i);
    if (c is null) return Results.NotFound("No container found with id: " + i);
    if (c.Status is StatusEnum.Running | c.Status is StatusEnum.Starting) return Results.BadRequest("Container is running or starting");
    //do some container start up here...

    c.Status = StatusEnum.Starting;

    return Results.Ok("Server is starting...");
});

app.MapPost("/container/{i}/stop", async (int i, UserContainerDb db) =>
{
    var c = await db.Containers.FindAsync(i);
    if (c is null) return Results.NotFound("No container found with id: " + i);
    if (c.Status is StatusEnum.NotRunning | c.Status is StatusEnum.Stopping) return Results.BadRequest("Container is not running");
    //do some container stop here...
    c.Status = StatusEnum.Stopping;

    return Results.Ok("Server is stopping...");
});

app.MapGet("/", () => Results.Redirect("/loginz")).AllowAnonymous();
app.MapGet("/loginz", () => Results.File("login.html", "text/html")).AllowAnonymous();
app.MapGet("/homez", () => Results.File("home.html", "text/html"));
app.MapGet("/adminsz", () => Results.File("admin.html", "text/html")).RequireAuthorization("AdminPolicy");
app.MapGet("/forbidden", () => Results.File("forbidden.html", "text/html")).AllowAnonymous();

app.MapGet("/debugbind", (IOptions<CookieAuthOptions> options) =>
{
    var c = options.Value;

    return Results.Json(new
    {
        LoginPath = c.LoginPath,
        LogoutPath = c.LogoutPath,
        AccessDeniedPath = c.AccessDeniedPath,
        ExpireTimeSpan = c.ExpireTimeSpan,
        SlidingExpiration = c.SlidingExpiration,
        CookieHttpOnly = c.CookieHttpOnly
    });
});

app.MapGet("/debugfinal", (IOptions<CookieAuthenticationOptions> options) =>
{
    var c = options.Value;

    return Results.Json(new
    {
        LoginPath = c.LoginPath,
        LogoutPath = c.LogoutPath,
        AccessDeniedPath = c.AccessDeniedPath,
        ExpireTimeSpan = c.ExpireTimeSpan,
        SlidingExpiration = c.SlidingExpiration,
        HttpOnly = c.Cookie.HttpOnly
    });
});

app.UseHttpsRedirection();
app.Run();