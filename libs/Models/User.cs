using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

public class User
{
    [Key] public int Id { get; set; }
    public string? DisplayName { get; set; }
    [Required][EmailAddress] public string Email { get; set; } = null!;
    [Required] public string Role {get;set;} = null!;
    [Required] public string PasswordHash { get; set; } = null!;
    public Guid? Sid {get; set;}
    public bool IsEmailVerified {get;set;} = false;
    ICollection<Container> Containers { get;} = [];
}
