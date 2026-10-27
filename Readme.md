# Readme

Wilkommen zu meinem kleinen Webapp-Projekt

Das Ziel dieser Anwendung ist es einen Nutzer, nachdem er sich registriert und eingeloggt hat, Zugriff auf vorgefertigte Containerimages zu erhalten (z.B. Gameserver), diese zu kontrollieren und für eigene Zwecke zu nutzen.

Um die Sicherheit zu gewährleisten, beschäftige ich mich in dieser Anwendung hauptsächlich mit Authentification and Authorization und dem sicheren Abarbeiten von Httprequests mithilfe eines Backgroundservices, der die Requests sequenziell abarbeitet.

Nachfolgend versuche ich den Verlauf einer einzelnen Anfrage zu erläutern, um sich so ein besseres machen zu können welche Gedanken man sich bei der sicheren Umsetzung des Projekts machen muss.

## Wahl des Frameworks

.NET bietet dank des riesigen, auf Enterprise Anwendungen ausgerichteten ASP.NET CORE Frameworks eine sichere sowie mühelose Umsetzung der oben genannten Funktionen. Die schwierigkeit besteht einzig darin die gigantische Oberfläche zu verstehen (was dank AI jedoch wesentlich leichter geworden ist)

Nachdem ein Request eingegangen ist, wird er von der Pipeline bearbeitet.
Zuerst wird die Anfrage, mithilfe von ASP.NET, in ein HttpContext Objekt umgewandelt.
In diesem Fall wird dieser Kontext von der Authentication Middleware untersucht, um festzustellen ob bereits ein Cookie vorhanden ist. Falls nicht, kann der User nur die Login Page besuchen.
        Falls ein Cookie vorhanden ist, wird dies per default nur auf expiration time geprüft. (FALSCH, nachlesen in [sourcecode](https://source.dot.net/Microsoft.AspNetCore.Authentication.Cookies/R/bea8bd91cacbb03d.html))
Nach der erfolgreichen Authorisierung wird der Request von der Authorization Middleware bearbeitet.

## Dependency Injection mit IServiceProvider

s

## Configuration in .NET CORE with IOptions and DI

**Term definition**

s

## Understanding Auth in .NET CORE

**Term definition**

1. **Authentication Scheme** Das ist der Begriff mit dem der AuthenticationHandler assoziert wird plus Einstellungen
2. **Authentication Handler** Der Handler ist die Baseclass für meinen CookieAuthenticationHandler in AddCookie(...) und besitzt die Logik des Authenticationsprozesses. Besitzt Methoden zum overriden wie zum beispiel:
    - HandleAuthenticateAsync: nimmt den Cookie vom Request und prüft ob das Cookie gültig ist. returnd succes/fail (basierend unter anderem auf den OnValidatePrincipal Hook, aber auch options vom addcookie call)
    - HandleSignInAsync: Set cookie in response. Beinhaltet vor allem das AuthenticationTicket(bestehnd aus ClaimsPrincipal und AuthScheme) (und save Encryption Key to data store(knowledge außerhalb meiner reichweite))
    - HandleSignOutAsync: nicht safe. alles was ich weiß es, es wird nicht gelöscht, sondern nur ein Set-Cookie header zurückgesendet der dem Browser sagt dass cookie expired ist.
