using Microsoft.EntityFrameworkCore;
using Models;

namespace WebApi.Services;
public class UserContainerDb(DbContextOptions<UserContainerDb> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
    }
    public DbSet<Container> Containers { get; set; }
    public DbSet<User> Users { get; set; }
}
