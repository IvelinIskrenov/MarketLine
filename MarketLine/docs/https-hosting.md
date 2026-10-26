# HTTPS and production secrets

MarketLine turns on HTTP-to-HTTPS redirection and HSTS only when `ASPNETCORE_ENVIRONMENT=Production`. Local Development on `http://localhost:5045` stays on HTTP.

TLS is terminated by the host or reverse proxy. This app does not create or install certificates.

## 1. TLS certificate

Use a valid certificate for the public hostname (not a self-signed cert).

- **Let's Encrypt** (nginx, Caddy, or a host panel): issue a certificate for `www.example.com`.
- **IIS / Windows**: bind the certificate on port 443 in IIS (or HTTP.sys).
- **Azure App Service**: turn on the managed certificate (or upload a PFX) and enable **HTTPS Only**.

## 2. Reverse proxy headers

If nginx, IIS ARR, or another proxy terminates TLS, the app must see `X-Forwarded-Proto: https` so cookies and redirects stay on HTTPS.

nginx example:

```
server {
    listen 443 ssl;
    server_name www.example.com;
    ssl_certificate     /etc/ssl/certs/fullchain.pem;
    ssl_certificate_key /etc/ssl/private/privkey.pem;

    location / {
        proxy_pass http://127.0.0.1:5045;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}

server {
    listen 80;
    server_name www.example.com;
    return 301 https://$host$request_uri;
}
```

IIS: install the certificate on the site, require SSL if the site is public, and forward `X-Forwarded-Proto` when using ARR.

## 3. Environment variables on the host

Set these in the hosting panel, systemd unit, IIS app pool, or Azure Application Settings. Do not put them in git.

| Variable | Purpose |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Must be `Production` |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Cors__AllowedOrigins__0` | Public origin, e.g. `https://www.example.com` |

Example (Windows):

```
setx ConnectionStrings__DefaultConnection "Server=...;Database=marketline_database;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=False"
```

## 4. Local development

Visual Studio / `dotnet run` with the Development profile does not require HTTPS. Optional HTTPS is the `https` launch profile (`https://localhost:7088`).

Local SQL can stay in `appsettings.Development.json` or in user secrets:

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=marketline_database;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
```

If SQL is unreachable in Development, the shop falls back to the in-memory database. That fallback is disabled in Production.
