using System;
using WebApi.Models;

namespace WebApi.Services;

public interface IUserAuthService<T>
{
    public Task<LoginResult> UserLoginAsync(LoginDTO login);
    //    public Task<LoginResult> UserLogoutAsync(LoginDTO login);
    //    public Task<LoginResult> UserRegisterAsync(LoginDTO login);
    //    public Task<LoginResult> UserDeleteAccAsync(LoginDTO login);


}
