# Easy Moon on RunASP.NET

This project is **not deployed** by this document. It prepares a folder you upload yourself to the free RunASP.NET / MonsterASP site (`*.runasp.net`).

Architecture used: **one website**. ASP.NET Core 9 serves the API and the Angular 19 production files. The browser calls `/api/...` on the same origin, so production does not use `localhost`.

Active host project: `src/Host/ErpClink.Api` (`net9.0`).  
Angular app: `frontend/erpclink-web` (Angular 19).  
Active database modules migrated on startup: Administration + Laser Clinic. Existing migrations are reused.

---

## 1. Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download) on your PC (to publish)
- Node.js 20 (to build Angular)
- A free account at [MonsterASP.NET / RunASP](https://www.monsterasp.net/ASP.NET-Freehosting/)
- One website on the free plan (subdomain `something.runasp.net`)
- One SQL Server database created in the same control panel

Free-plan limits that affect this app: one website, one database (about 1 GB), 256 MB RAM, limited traffic, and **no HTTPS**. The public link is `http://YOUR-SITE.runasp.net`.

---

## 2. RunASP.NET website creation

1. Sign in to the hosting control panel.
2. Create a website. Note the subdomain, for example `easymoon.runasp.net`.
3. Confirm the site runtime is **ASP.NET Core / .NET 9** (not a static-only site).
4. Do not upload the Angular `dist` folder by itself. The site must run `ErpClink.Api.dll`.

---

## 3. Database setup

1. In the control panel, create the one included SQL Server database.
2. Copy the connection string the panel shows.
3. Do not create a new schema and do not delete migrations.
4. Put that connection string only in the hosting settings (next section), not in git.

The application reads:

`ConnectionStrings:DefaultConnection`

---

## 4. Required application settings

Set these in the site’s **environment variables** (ASP.NET Core settings).  
Names use a double underscore. Values are yours; examples are placeholders.

| Name | Value |
|------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` (also set by `web.config`) |
| `ConnectionStrings__DefaultConnection` | `YOUR_PRODUCTION_SQL_CONNECTION_STRING` |
| `Authentication__Jwt__SigningKey` | `YOUR_PRODUCTION_JWT_KEY` (32+ random characters) |
| `Authentication__InitialAdmin__Email` | `admin@erpclink.local` (or your email) |
| `Authentication__InitialAdmin__Password` | a strong password you choose |
| `Authentication__InitialReceptionist__UserName` | `socialmedia` |
| `Authentication__InitialReceptionist__Password` | a strong password you choose |

`appsettings.Production.template.json` is copied into the publish folder as `appsettings.Production.json` with **empty** secrets. It overrides the local `appsettings.json` development database and development JWT key. The real values must come from the panel. Empty admin passwords mean the seeder skips those users until you set them and restart.

Do not commit a filled `appsettings.Production.json`. That filename is gitignored.

`FRONTEND_URL` stays empty. The Angular app and the API share the site origin, so the browser does not make a cross-origin call.

---

## 5. Backend publish command

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-runasp.ps1
```

That script runs the Angular production build and:

```powershell
dotnet publish src/Host/ErpClink.Api/ErpClink.Api.csproj -c Release -o artifacts/runasp --self-contained false
```

Target framework: **net9.0**, framework-dependent (RunASP already has the shared runtime). No self-contained runtime is added.

---

## 6. Angular production build

The script sets `API_BASE_URL` to empty before `npm run build:prod`.  
`scripts/set-api-url.mjs` writes `apiBaseUrl: ""`, so requests go to `/api/...` on the same site.

Output used: `frontend/erpclink-web/dist/erpclink-web/browser`.

The script restores `src/environments/environment.ts` afterward so the empty production value is not a git change. Development stays `http://localhost:5284` in `environment.development.ts` only.

---

## 7. Upload / deployment structure

Publish folder: `artifacts/runasp`.

Zip **the contents** of that folder (the files, not an extra parent directory) and upload them to the RunASP site root. Their file manager calls that folder `wwwroot`. After unzip, the site root must look like this:

```text
ErpClink.Api.dll
ErpClink.Api.exe          (if present)
web.config
appsettings.json
appsettings.Production.json
wwwroot/
  index.html
  *.js
  *.css
```

`wwwroot/index.html` is the Angular app. The DLL stays **beside** that `wwwroot` folder, not inside it.

Restart the site / application pool after upload and after any setting change.

---

## 8. Domain / subdomain configuration

Use the free subdomain from the panel, for example:

`http://YOUR-SITE.runasp.net`

Open `/` and `/app/dashboard`. Both must return the Angular page. `/health` must return JSON `{"status":"Healthy"}`.

There is no separate frontend URL on the free plan.

---

## 9. CORS configuration

Same origin: Angular is hosted by this ASP.NET site, so API calls are `/api/...` and **do not need CORS**.

The existing policy still allows only:

- `http://localhost:4200`
- `https://localhost:4200`
- origins listed in `FRONTEND_URL`, if you set any

`AllowAnyOrigin()` is not used. Leave `FRONTEND_URL` empty for RunASP.

---

## 10. Database migration process

On startup the existing code already applies migrations:

- Administration: `AdministrationDbSeeder` calls `Database.MigrateAsync`
- Laser Clinic: `MigrateLaserClinicModuleAsync`

No new migration is required for this hosting change. Do not delete or recreate the database to deploy.

First start against the RunASP database creates the tables and, if the initial-user settings are present, seeds the admin and receptionist accounts.

Optional manual update from your PC (only if you prefer not to rely on startup), using the host’s connection string:

```powershell
dotnet ef database update `
  --project src/Modules/Administration/ErpClink.Modules.Administration.Infrastructure `
  --startup-project src/Host/ErpClink.Api `
  --connection "YOUR_PRODUCTION_SQL_CONNECTION_STRING"

dotnet ef database update `
  --project src/Modules/LaserClinic/ErpClink.Modules.LaserClinic.Infrastructure `
  --startup-project src/Host/ErpClink.Api `
  --connection "YOUR_PRODUCTION_SQL_CONNECTION_STRING"
```

---

## 11. First login verification

1. Restart the site after the environment variables are saved.
2. Open `http://YOUR-SITE.runasp.net`.
3. Sign in with `Authentication__InitialAdmin__Email` and `Authentication__InitialAdmin__Password`.
4. Confirm `/health` is JSON, and a screen such as `/app/dashboard` reloads without 404.
5. Receptionist login uses the username and password you set (`socialmedia` by default).

---

## 12. Troubleshooting

| What you see | What to check |
|--------------|----------------|
| 500.30 or 500.31 | Site must be ASP.NET Core on .NET 9. Upload the publish output, not only Angular files. |
| 404 on `/` | `wwwroot/index.html` must sit under the site root next to `ErpClink.Api.dll`. Re-run the publish script and re-upload. |
| 404 on `/app/dashboard` after refresh | Same as above. The API fallback serves `index.html` for non-API paths. `/api/...` stays on controllers. |
| Login fails / API errors in the browser | `ConnectionStrings__DefaultConnection` and `Authentication__Jwt__SigningKey` must be set, then restart. |
| App starts then crashes on SQL | The connection string is wrong, or the database firewall blocks the site. Use the string from the panel. |
| Still using LocalDB | `ASPNETCORE_ENVIRONMENT` is not `Production`, so `appsettings.Production.json` did not override the dev connection string. |

---

## 13. Rollback

1. Keep the previous zip of `artifacts/runasp` before you upload a new one.
2. To roll back, upload that older zip again with overwrite, then restart the application pool.
3. Do not drop the SQL database. Migrations already applied stay compatible with the previous build of this app unless a later release adds a migration you have not shipped.
