using System.Security.Claims;

public class CookieAuthenticationHandlerEx : CookieAuthenticationHandler
{
    private Task<AuthenticateResult> EnsureCookieTicket()
    {
        if (_readCookieTask == null)
        {
            _readCookieTask = ReadCookieTicket();
        }

        return _readCookieTask;
    }

    private async Task<AuthenticateResult> ReadCookieTicket()
    {
        string requestCookie = base.Options.CookieManager.GetRequestCookie(base.Context, base.Options.Cookie.Name);
        if (string.IsNullOrEmpty(requestCookie))
        {
            return AuthenticateResult.NoResult();
        }

        AuthenticationTicket authenticationTicket = base.Options.TicketDataFormat.Unprotect(requestCookie, GetTlsTokenBinding());
        if (authenticationTicket == null)
        {
            return AuthenticateResults.FailedUnprotectingTicket;
        }

        if (base.Options.SessionStore != null)
        {
            Claim claim = authenticationTicket.Principal.Claims.FirstOrDefault((Claim c) => c.Type.Equals("Microsoft.AspNetCore.Authentication.Cookies-SessionId"));
            if (claim == null)
            {
                return AuthenticateResults.MissingSessionId;
            }

            authenticationTicket = await base.Options.SessionStore.RetrieveAsync(claim.Value, base.Context, base.Context.RequestAborted);
            if (authenticationTicket == null)
            {
                return AuthenticateResults.MissingIdentityInSession;
            }

            _sessionKey = claim.Value;
        }

        DateTimeOffset utcNow = base.TimeProvider.GetUtcNow();
        DateTimeOffset? expiresUtc = authenticationTicket.Properties.ExpiresUtc;
        if (expiresUtc.HasValue && expiresUtc.Value < utcNow)
        {
            if (base.Options.SessionStore != null)
            {
                await base.Options.SessionStore.RemoveAsync(_sessionKey, base.Context, base.Context.RequestAborted);
                _sessionKey = null;
            }

            return AuthenticateResults.ExpiredTicket;
        }

        return AuthenticateResult.Success(authenticationTicket);
    }


    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        AuthenticateResult result = await EnsureCookieTicket();
        if (!result.Succeeded)
        {
            return result;  
            // returns to AuthenticationService:AuthenticateAsync:l53 und 
            // diese returns to AuthenticationMiddleware:Invoke:l54
        }

        await CheckForRefreshAsync(result.Ticket);
        CookieValidatePrincipalContext context = new CookieValidatePrincipalContext(base.Context, base.Scheme, base.Options, result.Ticket);
        await Events.ValidatePrincipal(context);
        if (context.Principal == null)
        {
            return AuthenticateResults.NoPrincipal;
        }

        if (context.ShouldRenew)
        {
            RequestRefresh(result.Ticket, context.Principal);
        }

        return AuthenticateResult.Success(new AuthenticationTicket(context.Principal, context.Properties, base.Scheme.Name));
    }

    private async Task CheckForRefreshAsync(AuthenticationTicket ticket)
    {
        DateTimeOffset utcNow = base.TimeProvider.GetUtcNow();
        DateTimeOffset? issuedUtc = ticket.Properties.IssuedUtc;
        DateTimeOffset? expiresUtc = ticket.Properties.ExpiresUtc;
        bool isRefreshAllowed = ticket.Properties.AllowRefresh ?? true;
        if (issuedUtc.HasValue && expiresUtc.HasValue && base.Options.SlidingExpiration && isRefreshAllowed)
        {
            TimeSpan timeSpan = utcNow.Subtract(issuedUtc.Value);
            TimeSpan timeSpan2 = expiresUtc.Value.Subtract(utcNow);
            CookieSlidingExpirationContext eventContext = new CookieSlidingExpirationContext(base.Context, base.Scheme, base.Options, ticket, timeSpan, timeSpan2)
            {
                ShouldRenew = timeSpan2 < timeSpan
            };
            await Events.CheckSlidingExpiration(eventContext);
            if (eventContext.ShouldRenew)
            {
                RequestRefresh(ticket);
            }
        }
    }
    
}