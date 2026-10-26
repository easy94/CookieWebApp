using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

public class Container
{
    [Key] public int Id { get; set; }
    public StatusEnum Status { get; set; }
    public string? Name { get; set; }
    public int UserId { get; set; }
}
