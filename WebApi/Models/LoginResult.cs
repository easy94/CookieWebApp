using System;
using Models;

namespace WebApi.Models;

public record LoginResult(bool Success, string? Error, User? User);
