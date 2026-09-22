# نشر Easy Moon للعميل (لينك عام)

`http://localhost:4200` يشتغل عندك بس على جهازك.  
عشان تدي العميل لينك يفتح من أي مكان، لازم تنشر على الإنترنت (Render مجاني).

## اللينكات بعد النشر (أمثلة)

| الخدمة | اللينك |
|--------|--------|
| الموقع للعميل | `https://easymoon-web.onrender.com` |
| الـ API | `https://easymoon-api.onrender.com` |
| صحة الـ API | `https://easymoon-api.onrender.com/health` |

اللينك اللي تبعته للعميل = **رابط الموقع (Static Site)** مش localhost.

---

## المطلوب منك (مرة واحدة)

### 1) ارفع المشروع على GitHub
1. اعمل حساب/سجّل دخول على [github.com](https://github.com)
2. New repository → اسم مثلاً `easymoon-laser`
3. من مجلد المشروع:

```powershell
cd d:\Abdel\abdelrahman\Agent\ErpClink
git remote add origin https://github.com/YOUR_USER/easymoon-laser.git
git push -u origin main
```

### 2) قاعدة بيانات Azure SQL
- أنشئ Database مجانية على Azure
- انسخ Connection String
- افتح Firewall عشان Render يقدر يتصل

### 3) Backend على Render (Web Service + Docker)
- Root: `.`
- Dockerfile: `Dockerfile`
- Environment:
  - `ConnectionStrings__DefaultConnection` = Azure connection string
  - `ASPNETCORE_ENVIRONMENT` = `Production`
  - `Authentication__Jwt__SigningKey` = مفتاح طويل عشوائي (32+ حرف)
  - `FRONTEND_URL` = رابط الـ Static Site بعد ما يتعمل (خطوة 4)
  - `Authentication__InitialAdmin__Email` / `__Password` (اختياري)
  - `Authentication__InitialReceptionist__UserName` = `socialmedia`
  - `Authentication__InitialReceptionist__Password` = `EasyMoon@2026`

Health check: `/health`

### 4) Frontend على Render (Static Site)
- Root directory: `frontend/erpclink-web`
- Build: `npm install && npm run build:prod`
- Publish: `dist/erpclink-web/browser`
- Environment:
  - `API_BASE_URL` = `https://YOUR-API.onrender.com` (بدون / في الآخر)

بعد البناء، انسخ رابط الموقع وبعته للعميل.

### 5) اربط CORS
ارجع للـ API وضَع `FRONTEND_URL` = رابط الموقع بالظبط، ثم Redeploy.

---

## دخول الاختبار بعد النشر

**Super Admin**
- `admin@erpclink.local` / `ChangeMe!Admin123`  
  (أو القيم اللي حطيتها في Environment)

**موظفة السوشيال**
- `socialmedia` / `EasyMoon@2026`

---

التفاصيل الكاملة: [`DEPLOYMENT.md`](./DEPLOYMENT.md)
