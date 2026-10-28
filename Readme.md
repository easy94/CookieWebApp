# Readme

Wilkommen zu meinem kleinen Webapp-Projekt

Das Ziel dieser Anwendung ist es einen Nutzer, nachdem er sich registriert und eingeloggt hat, Zugriff auf vorgefertigte Containerimages zu erhalten (z.B. Gameserver), diese zu kontrollieren und für eigene Zwecke zu nutzen.

Um die Sicherheit zu gewährleisten, beschäftige ich mich in dieser Anwendung hauptsächlich mit Authentification and Authorization und dem sicheren Abarbeiten von Httprequests mithilfe eines Backgroundservices, der die Requests sequenziell abarbeitet.

Nachfolgend versuche ich den Verlauf einer einzelnen Anfrage zu erläutern, um sich so ein besseres machen zu können welche Gedanken man sich bei der sicheren Umsetzung des Projekts machen muss.

Nachdem ein Request eingegangen ist, wird er von der Pipeline bearbeitet.
Zuerst wird die Anfrage, mithilfe von ASP.NET, in ein HttpContext Objekt umgewandelt.
In diesem Fall wird dieser Kontext von der Authentication Middleware untersucht, um festzustellen ob bereits ein Cookie vorhanden ist.
Die default implementation des CookieAuthenticationHandler's returnt basierend auf den Http Kontext entweder Fail/Success und gibt an, ob der Cookie erneuert werden muss.
Falls ein Cookie vorhanden ist, wird dies abhängig vom CookieScheme auf bestimmte Eigenschaften geprüft. (FALSCH, nachlesen in [sourcecode](https://source.dot.net/Microsoft.AspNetCore.Authentication.Cookies/R/bea8bd91cacbb03d.html))
Nach der erfolgreichen Authorisierung wird der Request von der Authorization Middleware bearbeitet.

ClaimsPrincipal = All the Identities of the "User"

CookieAuthenticationHandler returns Sucess/NoResult/ExpiredTicket/etc.
Authentication Middleware kann mehrmals runnen, basierend auf den default global scheme
und auf die Authorization Policies der einzelnen Endpunkte
Der CookieAuthHandler prüft folgendes:

- ist Cookie vorhanden? -> reuturn NoResult
- wurde Cookie verändert? -> return FailToUnprotect
- expired? -> return Expired
- Success

Bei Success wird ClaimsPrincipal constructet dann next Middleware call, bei allem anderen Result direkt weiter 

Der Flow ist: AuthNmiddleware -> AuthService -> AuthHandler -> AuthZmiddleware -> AuthZService -> PolicyEvaluator(endpoint policies) -> AuthHandler ->

## Dependency Injection mit IServiceProvider

Endpoints and Httpcontext get resolved for each Request (scoped).
Transient and Singleton Services can not depend on Scoped serviced.
Deshalb ist Httpcontextacessor nötig in manchen fällen.

## Configuration in .NET CORE with IOptions and DI

first add the appsettings.json (default), then bind its section to CookieAuthOptions,
meaning that we tell the configure service where to bind the method's arguments against (CookieAuthOptions in this case).
Then derive from the IOptionsConfigure Service (my CookieAuthOptionsConfigure) and use it to configure any requested CookieAuthenticationsOptions.

IOptionsConfigure is the Singleton service way of the options pattern.
You can also derive from IOptionsNamedConfigure, IOptionsMonitor or IOptions... to extend its capabilities

## Understanding Auth in .NET CORE

1. **Authentication Scheme** Das ist der Begriff mit dem der AuthenticationHandler assoziert wird plus Einstellungen
2. **Authentication Handler** Der Handler ist die Baseclass für meinen CookieAuthenticationHandler in AddCookie(...) und besitzt die Logik des Authenticationsprozesses. Besitzt Methoden zum overriden wie zum beispiel:
    - HandleAuthenticateAsync: nimmt den Cookie vom Request und prüft ob das Cookie gültig ist. returnd succes/fail (basierend unter anderem auf den OnValidatePrincipal Hook, aber auch options vom addcookie call)
    - HandleSignInAsync: Set cookie in response. Beinhaltet vor allem das AuthenticationTicket(bestehnd aus ClaimsPrincipal und AuthScheme) (und save Encryption Key to data store(knowledge außerhalb meiner reichweite))
    - HandleSignOutAsync: nicht safe. alles was ich weiß es, es wird nicht gelöscht, sondern nur ein Set-Cookie header zurückgesendet der dem Browser sagt dass cookie expired ist.
