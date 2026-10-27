using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebApi.Services;
using WebApi.Models;
using Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using WebApi.Options;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Http.HttpResults;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionstring = builder.Configuration.GetConnectionString("UserContainerDb") ?? "Data Source=../usercontainer.db";
builder.Services.AddSqlite<UserContainerDb>(connectionstring);

//add the appsettings.json (default in line15), then bind its section to CookieAuthOptions, then register it via ConfigureOptions 
//so when AddCookie() gets called it gets resolved
builder.Services.Configure<CookieAuthOptions>(builder.Configuration.GetSection(nameof(CookieAuthOptions)));
builder.Services.ConfigureOptions<CookieAuthOptionsConfigure>();

builder.Services.AddTransient<UserAuthService>();
builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddHttpClient();

// The actual cookie behavior is configured from the appsettings JSON via
// CookieAuthOptions + CookieAuthOptionsConfigure, which are registered above.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Intentionally left empty: values are applied by
        // builder.Services.ConfigureOptions<CookieAuthOptionsConfigure>()
        // and the bound CookieAuthOptions section.
    });

builder.Services.AddValidation();

builder.Services.AddAuthorization(c =>
{
    c.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser().Build();

    c.AddPolicy("AdminPolicy", p =>
    p.RequireAuthenticatedUser()
    .RequireRole("admin").Build());
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

app.MapPost("/register", async (LoginDTO login, UserContainerDb db, PasswordHasher<User> pwHasher) =>
{
    //check if supplied email exists already
    if (await db.Users.AnyAsync(u => u.Email == login.Email)) return Results.BadRequest("User with this email already exists");

    var newuser = new User
    {
        Role = "user",
        Email = login.Email
    };
    newuser.PasswordHash = pwHasher.HashPassword(newuser, login.Password);

    //register him
    var u = await db.Users.AddAsync(newuser);
    await db.SaveChangesAsync();

    return Results.LocalRedirect("/", true, false);

}).AllowAnonymous();

app.MapPost("/login", async (LoginDTO login, HttpContext httpContext, UserAuthService auth) =>
{
    LoginResult loginResult = await auth.UserLoginAsync(login);
    if (!loginResult.Success){
        return Results.BadRequest($"User: {loginResult.User}: {loginResult.Error}");
    }

    return Results.Redirect($"/home.html");

}).AllowAnonymous();

app.MapPost("/logout", (HttpContext httpContext) =>
{
    httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login.html");
});

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

app.MapGet("/login.html", () => Results.File("/www.root/login.html")).AllowAnonymous();
app.MapGet("/home.html", () => Results.File("/www.root/home.html"));
app.MapGet("/adminspace.html", () => Results.File("/www.root/adminspace.html")).RequireAuthorization("AdminPolicy");

app.UseStaticFiles();
app.UseHttpsRedirection();

app.Run();