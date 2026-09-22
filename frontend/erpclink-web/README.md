# ErpClink Web (Angular)

Production clinic ERP frontend for the ErpClink ASP.NET Core API.

## Quick start

```bash
# API must listen on http://localhost:5284 (see launchSettings)
dotnet run --project ../../src/Host/ErpClink.Api

npm install
npm start
```

App: http://localhost:4200 — API proxied via `proxy.conf.json`.

See `docs/frontend.md` in the repository root for architecture details.
