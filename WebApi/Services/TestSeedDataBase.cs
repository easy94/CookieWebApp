using System;
using Models;

namespace WebApi.Services;

public static class TestSeedDataBase
{
    public static void SeedDatabase(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<UserContainerDb>();
        var pwh = sp.GetRequiredService<Microsoft.AspNetCore.Identity.PasswordHasher<User>>();

        var user = new User
        {
            Email = "lucas123@irgendwas.de",
            PasswordHash = "123456",
            Role = "admin"
        };
        user.PasswordHash = pwh.HashPassword(user, user.PasswordHash);
        var userCheck = db.Users.FirstOrDefault(p => p.Email == user.Email);

        if (userCheck is null)
        {
            var result = db.Users.Add(user);
            System.Console.WriteLine(result);
            db.SaveChanges();
        }
    }
}
