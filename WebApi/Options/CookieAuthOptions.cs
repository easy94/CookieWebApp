using System;

namespace WebApi.Options;

public class CookieAuthOptions
{
    public string? LoginPath { get; set; }
    public string? LogoutPath { get; set; }
    public string? AccessDeniedPath { get; set; }
    public TimeSpan ExpireTimeSpan { get; set; }
    public bool SlidingExpiration { get; set; }
    public bool CookieHttpOnly { get; set; }
}
