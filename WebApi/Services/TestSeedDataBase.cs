using System;
using Microsoft.EntityFrameworkCore;
using Models;

namespace WebApi.Services;

public static class TestSeedDataBase
{
    public static void SeedDatabase(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<UserContainerDb>();
        var pwh = sp.GetRequiredService<Microsoft.AspNetCore.Identity.PasswordHasher<User>>();

        db.Users.RemoveRange(db.Users);

        var users = new List<User>([
            new User{ Email = "lucas@meme.de", PasswordHash = "123456", Role = "admin"},
            new User { Email = "nobody@meme.de", PasswordHash = "654321", Role = "user"}]
        );

        
        foreach (var u in users)
        {
            u.PasswordHash = pwh.HashPassword(u, u.PasswordHash);
            db.Users.Add(u);
        }

        db.SaveChanges();
    }
}
