# Easy Moon Laser Clinic — Free Deployment Guide

Deploy the **existing** Easy Moon application online for clinic testing using:

| Layer | Host | Notes |
|--------|------|--------|
| Frontend | Render **Static Site** (free) | Angular 19 production build |
| Backend | Render **Web Service** (free) | ASP.NET Core 9 API via Docker |
| Database | **Azure SQL** free offer | You create this separately |

This guide does **not** change business logic or UI. Follow the steps in order.

---

## Architecture (what you already have)

| Item | Location / value |
|------|------------------|
| Angular app | `frontend/erpclink-web` |
| API host | `src/Host/ErpClink.Api` |
| .NET | **9.0** |
| Angular | **19.x** |
| EF Core | **9.0.x** + **SQL Server** |
| Auth | JWT Bearer (`Authorization` header), tokens in `localStorage` |
| Active modules | Administration + Laser Clinic |
| Migrations | Applied **automatically on API startup** |
| Dockerfile | repository root `Dockerfile` |
| Angular prod output | `frontend/erpclink-web/dist/erpclink-web/browser` |

Default local admin (seeded if missing — **change in production**):

- Email: `admin@erpclink.local`
- Password: `ChangeMe!Admin123`

---

## STEP 1 — Push the project to GitHub

```bash
cd /path/to/ErpClink
git status
git add -A
git commit -m "Prepare Easy Moon for Render + Azure SQL deployment"
git remote add origin https://github.com/<YOUR_USER>/<YOUR_REPO>.git   # if needed
git push -u origin HEAD
```

Do **not** commit real passwords, JWT signing keys, or Azure connection strings.

---

## STEP 2 — Create Azure SQL Database (free offer)

1. In Azure Portal, create a SQL Server + Database using the free/offer tier available to you.
2. Note the server name, database name, SQL login, and password.
3. Under the SQL server **Networking / Firewalls**:
   - Allow Azure services (recommended).
   - For Render free testing, you typically need to allow the API to connect (often temporarily allow public access or add Render egress IPs). Without this, the API will fail to reach the database.

---

## STEP 3 — Get the Azure SQL connection string

Use an ADO.NET connection string similar to:

```text
Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=YOUR_DB;User ID=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;
```

You will paste this into Render as `ConnectionStrings__DefaultConnection` (double underscore).

---

## STEP 4 — Create the Render Backend Web Service

1. Render → **New** → **Web Service**.
2. Connect your GitHub repo.
3. Settings:

| Setting | Value |
|---------|--------|
| Runtime | **Docker** |
| Dockerfile path | `Dockerfile` |
| Docker context | repository root (`.`) |
| Region | closest to you / your Azure region |
| Instance | Free |

4. Health check path: `/health`

---

## STEP 5 — Configure backend environment variables

In the Render Web Service → **Environment**, add:

| Key | Example / notes |
|-----|------------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Azure SQL connection string from Step 3 |
| `FRONTEND_URL` | Your Angular Static Site URL (Step 8), e.g. `https://easymoon-web.onrender.com` — **no trailing slash**. Comma-separated if multiple. |
| `Authentication__Jwt__SigningKey` | Long random secret **≥ 32 characters** (required) |
| `Authentication__Jwt__Issuer` | `ErpClink` (optional; defaults from appsettings) |
| `Authentication__Jwt__Audience` | `ErpClink.Client` (optional) |
| `Authentication__InitialAdmin__Email` | e.g. `admin@yourclinic.com` |
| `Authentication__InitialAdmin__Password` | Strong password (seeded only if admin missing) |
| `Authentication__InitialAdmin__FullName` | e.g. `Clinic Admin` |
| `PORT` | Set automatically by Render — do not override |

`PORT` is provided by Render. The container binds to `0.0.0.0:$PORT`.

---

## STEP 6 — Deploy the backend

Deploy / wait for the first build. On successful start the API:

1. Connects to Azure SQL.
2. Runs EF Core migrations for Administration + Laser Clinic.
3. Seeds permissions / initial admin if needed.
4. Serves `GET /health` → `{"status":"Healthy"}`.

Swagger is **disabled** in Production (enabled only in Development).

---

## STEP 7 — Get the backend Render URL

Example:

```text
https://easymoon-api.onrender.com
```

Verify:

```text
https://easymoon-api.onrender.com/health
```

---

## STEP 8 — Create the Render Angular Static Site

> **Critical:** Use **New → Static Site**, not Web Service.  
> A Web Service (or a failed/empty Static Site) on `easymoon-web.onrender.com` returns plain `Not Found` with `x-render-routing: no-server`.

1. Render → **New** → **Static Site** (or apply [`render.yaml`](./render.yaml) Blueprint).
2. Connect the same GitHub repo.
3. Settings:

| Setting | Value |
|---------|--------|
| Root directory | `frontend/erpclink-web` |
| Build command | `npm install && npm run build:prod` |
| Start command | _(none — leave empty)_ |
| Publish directory | `dist/erpclink-web/browser` |

4. **Redirects/Rewrites** (Dashboard → Redirects/Rewrites, or via Blueprint `routes`):

| Source | Destination | Action |
|--------|-------------|--------|
| `/*` | `/index.html` | **Rewrite** |

Angular 19 outputs to `dist/erpclink-web/browser` (not `dist/erpclink-web`).  
`public/_redirects` is also copied into the publish folder for hosts that honor Netlify-style redirects.

---

## STEP 9 — Configure the Angular API URL

On the Static Site → **Environment**:

| Key | Value |
|-----|--------|
| `API_BASE_URL` | `https://easymoon-api.onrender.com` (your backend URL, **no trailing slash**) |

This is injected at **build time** into `environment.apiBaseUrl` by `scripts/set-api-url.mjs`.

After changing `API_BASE_URL`, **trigger a new Static Site deploy** (rebuild).

---

## STEP 10 — Configure backend CORS using the frontend URL

1. Copy the Static Site URL, e.g. `https://easymoon-web.onrender.com`.
2. Set backend env `FRONTEND_URL` to that exact origin (no path, no trailing slash).
3. Redeploy the backend (or restart) so CORS picks up the value.

Local development still allows `http://localhost:4200` and `https://localhost:4200`.

---

## STEP 11 — Apply EF Core migrations to Azure SQL

**Automatic (recommended):** migrations run when the API starts (`AdministrationDbSeeder` + `MigrateLaserClinicModuleAsync`).

**Manual (optional, from your machine):**

```bash
# Administration
dotnet ef database update \
  --project src/Modules/Administration/ErpClink.Modules.Administration.Infrastructure \
  --startup-project src/Host/ErpClink.Api \
  --connection "YOUR_AZURE_SQL_CONNECTION_STRING"

# Laser Clinic
dotnet ef database update \
  --project src/Modules/LaserClinic/ErpClink.Modules.LaserClinic.Infrastructure \
  --startup-project src/Host/ErpClink.Api \
  --connection "YOUR_AZURE_SQL_CONNECTION_STRING"
```

Do **not** delete existing migrations. Reuse them.

---

## STEP 12 — Login with the admin user

Use the Initial Admin email/password you set in Render env vars (or the seeded defaults if you left them).

Change the password after first login if you used the documented local default.

---

## STEP 13 — Test checklist

- [ ] Login
- [ ] Customers list / create / edit
- [ ] Appointments list / new / edit / cancel
- [ ] Appointment availability (real clinic hours + conflicts — **not mock**)
- [ ] Calendar
- [ ] Services / areas
- [ ] Offers (if used)
- [ ] Reports / dashboard pulses
- [ ] API auth (401 without token; success with token)
- [ ] Data persists after refresh (Azure SQL)

---

## Environment variables summary

### Backend (Render Web Service)

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=...
FRONTEND_URL=https://YOUR-FRONTEND.onrender.com
Authentication__Jwt__SigningKey=...
Authentication__InitialAdmin__Email=...
Authentication__InitialAdmin__Password=...
Authentication__InitialAdmin__FullName=...
```

`PORT` is automatic.

### Frontend (Render Static Site — build time)

```text
API_BASE_URL=https://YOUR-BACKEND.onrender.com
```

---

## Local development (unchanged)

- API: run `ErpClink.Api` (default local URL used by Angular: `http://localhost:5284`)
- UI: `cd frontend/erpclink-web && npm start` (proxy + CORS for localhost)
- Connection string: LocalDB / SQL Server via `appsettings.json` or user secrets — not for production

Example config without secrets: `src/Host/ErpClink.Api/appsettings.Example.json`

---

## Risks / warnings (free hosting)

1. **Render free Web Service sleeps** after idle time → first request is slow (cold start). Migrations + DB connect on wake.
2. **Azure SQL firewall** must allow Render to connect or the API will crash on startup.
3. **JWT SigningKey** must be set in production; otherwise the API will not start.
4. **FRONTEND_URL** must match the Static Site origin exactly or the browser will block API calls (CORS).
5. **API_BASE_URL** is baked into the JS bundle at build time — rebuild the Static Site after changing it.
6. Free tiers are for **testing**, not SLA production.
7. Do not commit real secrets. Angular `API_BASE_URL` is public by nature (not a secret).

---

## Useful commands (verification)

```bash
# Backend publish
dotnet publish src/Host/ErpClink.Api/ErpClink.Api.csproj -c Release -o ./artifacts/api

# Docker (from repo root)
docker build -t easymoon-api .

# Frontend production build
cd frontend/erpclink-web
npm install
API_BASE_URL=https://easymoon-api.onrender.com npm run build:prod
```
