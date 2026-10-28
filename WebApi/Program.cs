using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebApi.Services;
using WebApi.Models;
using Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using WebApi.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionstring = builder.Configuration.GetConnectionString("UserContainerDb") ?? "Data Source=../usercontainer.db";
builder.Services.AddSqlite<UserContainerDb>(connectionstring);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserAuthService<ICookieMarker>,UserAuthService>();
builder.Services.Configure<CookieAuthOptions>(builder.Configuration.GetSection(nameof(CookieAuthOptions)));
builder.Services.ConfigureOptions<CookieAuthOptionsConfigure>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<PasswordHasher<User>>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

//builder.Services.AddValidation();

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

//Endpoints are resolved for each request scope

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

    return Results.LocalRedirect("/home.html", true, false);

}).AllowAnonymous();

app.MapPost("/login", async (LoginDTO login, IUserAuthService<ICookieMarker> auth) =>
{
    LoginResult loginResult = await auth.UserLoginAsync(login);
    if (!loginResult.Success){
        return Results.BadRequest($"User: {loginResult.User}: {loginResult.Error}");
    }
    if(loginResult.User?.Role == "admin")
        return Results.File($"admin.html", "text/html");
    return Results.File($"home.html", "text/html");

}).AllowAnonymous();

app.MapPost("/logout", (HttpContext httpContext) =>
{
    httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.File("login.html", "text/html");
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

app.MapGet("/login.html", () => Results.File("login.html","text/html")).AllowAnonymous();
app.MapGet("/home.html", () => Results.File("home.html","text/html")).AllowAnonymous();
app.MapGet("/adminspace.html", () => Results.File("admin.html","text/html")).RequireAuthorization("AdminPolicy");
app.UseHttpsRedirection();

app.Run();