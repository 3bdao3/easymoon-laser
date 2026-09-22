# نشر Easy Moon للعميل (لينك عام)

## تشخيص سريع: `Not Found` على `easymoon-web.onrender.com`

الهيدر `x-render-routing: no-server` = **مفيش خدمة Live** مربوطة بالاسم ده (مش مشكلة Angular routes).

أشهر الأسباب:
1. الخدمة اتعملت كـ **Web Service** بدل **Static Site**
2. **Publish Directory** غلط (لازم ينتهي بـ `browser`)
3. الـ Build فشل ومفيش ملفات اتنشرت
4. الخدمة Suspended / اتحذفت والـ URL لسه موجود

---

## نوع المشروع

| جزء | التقنية | خدمة Render |
|-----|---------|-------------|
| Frontend | Angular 19 | **Static Site** → `easymoon-web` |
| Backend | ASP.NET Core 9 | **Web Service (Docker)** → `easymoon-api` |
| Database | SQL Server | Azure SQL (خارج Render) |

`localhost:4200` محلي فقط. لينك العميل = رابط الـ Static Site.

---

## إعدادات Render الصحيحة للفرونت (Static Site)

| Setting | Value |
|---------|--------|
| Service type | **Static Site** (مش Web Service) |
| Name | `easymoon-web` |
| Root Directory | `frontend/erpclink-web` |
| **Build Command** | `npm install && npm run build:prod` |
| **Start Command** | _(فاضي — Static Site مفيش Start)_ |
| **Publish Directory** | `dist/erpclink-web/browser` |
| Environment | `API_BASE_URL` = `https://easymoon-api.onrender.com` (بدون / في الآخر) |
| Redirects/Rewrites | Source `/*` → Destination `/index.html` → Action **Rewrite** |

بعد Deploy ناجح افتح: `https://easymoon-web.onrender.com/` → المفروض شاشة اللوجين / تحويل للدashboard.

### بديل: Web Service + Docker (لو مصر على Web Service)
- Dockerfile path: `Dockerfile.frontend`
- Docker context: `.`
- Build arg / env: `API_BASE_URL`
- Start: من الـ image (`serve` على `$PORT`) — مفيش Start Command يدوي

---

## إعدادات الـ API (Web Service + Docker)

| Setting | Value |
|---------|--------|
| Dockerfile | `Dockerfile` |
| Docker context | `.` |
| Health check | `/health` |
| Env | `ConnectionStrings__DefaultConnection`, `FRONTEND_URL=https://easymoon-web.onrender.com`, `Authentication__Jwt__SigningKey`, … |

التفاصيل: [`DEPLOYMENT.md`](./DEPLOYMENT.md) و Blueprint: [`render.yaml`](./render.yaml)

---

## دخول الاختبار

**Super Admin:** `admin@erpclink.local` / `ChangeMe!Admin123`  
**موظفة السوشيال:** `socialmedia` / `EasyMoon@2026`
