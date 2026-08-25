# RTS ERP — Production Deployment Guide

## Architecture

```
https://erp.rtegy.com     (Vercel — Next.js frontend)
          ↓
https://api.rtegy.com     (Railway / Render — .NET 8 API)
          ↓
SQL Server Database       (Railway addon / Azure SQL / Supabase)
```

---

## 1. Deploy the Backend

### Option A — Railway (recommended, free tier available)

1. Go to **railway.app** → New Project → Deploy from GitHub
2. Select repo: `ahmedanany115-lab/geovany-rts-crm`
3. Set **Root Directory**: `Backend`
4. Railway auto-detects the `Dockerfile`

**Add a SQL Server database:**
- In your Railway project → Add Plugin → **Microsoft SQL Server**
- Railway injects `${{SQLSERVER_URL}}` automatically

**Set these environment variables in Railway:**

| Variable | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | `Server=<railway-sql-host>;Database=RTSErpDb;User Id=<user>;Password=<pass>;TrustServerCertificate=True;` |
| `Jwt__SigningKey` | Generate with: `openssl rand -base64 48` (min 32 chars) |
| `Cors__AllowedOrigins__0` | `https://erp.rtegy.com` |

**Set a custom domain:** Railway project → Settings → Domains → `api.rtegy.com`

---

### Option B — Render

1. Go to **render.com** → New Web Service → Connect GitHub
2. Select repo, set **Root Directory**: `Backend`
3. Render auto-detects the `Dockerfile`
4. Set the same environment variables as above

---

### Option C — Azure Container Apps / Azure App Service

Use the provided `Dockerfile`. Set app settings:
- `ConnectionStrings__DefaultConnection`
- `Jwt__SigningKey`
- `Cors__AllowedOrigins__0` = `https://erp.rtegy.com`
- `ASPNETCORE_ENVIRONMENT` = `Production`

---

## 2. Configure Vercel (Frontend)

In **Vercel Dashboard** → Project → Settings → Environment Variables:

| Variable | Environment | Value |
|---|---|---|
| `NEXT_PUBLIC_API_BASE_URL` | Production | `https://api.rtegy.com/api/v1` |
| `NEXT_PUBLIC_API_BASE_URL` | Preview | `https://api.rtegy.com/api/v1` |
| `NEXT_PUBLIC_API_BASE_URL` | Development | `http://localhost:5210/api/v1` |

**Important:** After adding the variable, click **Redeploy** in Vercel.

---

## 3. DNS for api.rtegy.com

In your DNS provider (wherever rtegy.com is managed):

```
Type: CNAME
Name: api
Value: <railway-or-render-provided-domain>
TTL:  300
```

---

## 4. Verify Deployment

After deploying, test:

```bash
# Health check
curl https://api.rtegy.com/health

# Login (should return accessToken)
curl -X POST https://api.rtegy.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"sara.hassan@rts-erp.demo","password":"Admin@12345!"}'
```

Default admin credentials (seeded automatically):
- **Email:** `sara.hassan@rts-erp.demo`
- **Password:** `Admin@12345!`

---

## 5. Required Secrets Summary

| Secret | Where | Never commit |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Railway/Render env vars | ✓ |
| `Jwt__SigningKey` | Railway/Render env vars | ✓ |

The signing key must be at least 32 characters. Generate one:
```bash
openssl rand -base64 48
```

---

## 6. What Happens on First Start

The API automatically:
1. Runs all EF Core migrations (creates all tables)
2. Seeds: permissions, roles, 25 demo employees/users, chart of accounts, currencies (EGP/USD), tax rates, bank accounts, fiscal periods

No manual database setup required.
