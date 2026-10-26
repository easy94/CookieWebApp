# Plan and Thoughts

Stuck at creating a database migration so i can test the first endpoint in "WebApi" to "QueProcessor"'s Background service, which is not done yet either.

Solved Database migration issue. App does create "containers" and stop and start them simulated.

Next read about docker minecraft images and how to start them properly through the API

Next read about cookie authN and authZ for distinguishing between admin and user ops and relate containers to users

Plan for Cookie auth: AddCookie(general options). -> CustomizedAuthorizationCookiePolicyhandler -> Custom

PwHasher: hash the password once, then only look up the hash instead of storing the actual password
ClaimsPrincipal (special type): gets constructed on each request via ClaimsIdentity and HttpContext

**Email Verification** let java script send post request from login page on Failed attempt due to Email not verified to check for IsEmailVerified

## Readme

### Dependency Injection mit IServiceProvider

s

### Configuration in .NET CORE with IOptions and DI

**Term definition**

s

### Understanding Auth in .NET CORE

**Term definition**

1. **Authentication Scheme** Das ist der Begriff mit dem der AuthenticationHandler assoziert wird plus Einstellungen
2. **Authentication Handler** Der Handler ist die Baseclass für meinen CookieAuthenticationHandler in AddCookie(...) und besitzt die Logik des Authenticationsprozesses. Besitzt Methoden zum overriden wie zum beispiel:
    - HandleAuthenticateAsync: nimmt den Cookie vom Request und prüft ob das Cookie gültig ist. returnd succes/fail (basierend unter anderem auf den OnValidatePrincipal Hook, aber auch options vom addcookie call)
    - HandleSignInAsync: Set cookie in response. Beinhaltet vor allem das AuthenticationTicket(bestehnd aus ClaimsPrincipal und AuthScheme) (und save Encryption Key to data store(knowledge außerhalb meiner reichweite))
    - HandleSignOutAsync: nicht safe. alles was ich weiß es, es wird nicht gelöscht, sondern nur ein Set-Cookie header zurückgesendet der dem Browser sagt dass cookie expired ist.
