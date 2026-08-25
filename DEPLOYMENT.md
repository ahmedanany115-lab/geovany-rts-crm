# RTS ERP — Production Deployment Guide

## Architecture

```
https://erp.rtegy.com        (Vercel — Next.js frontend, already deployed)
          ↓
https://api.rtegy.com        (Railway — .NET 8 API via Docker)
          ↓
Supabase PostgreSQL           (already provisioned)
```

---

## Database — Supabase PostgreSQL

In your **Supabase dashboard** → Project → Settings → Database:

Copy the **Connection string** (URI format) or build it manually:

```
Host=db.<your-project>.supabase.co
Port=5432
Database=postgres
Username=postgres
Password=<your-db-password>
SSL Mode=Require
```

Full connection string for Railway:
```
Host=db.<project>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<pass>;SSL Mode=Require;Trust Server Certificate=true
```

> The API **auto-runs migrations** and **seeds data** on first startup — no manual SQL needed.

---

## Backend — Deploy on Railway

### Step 1 — Create Railway project

1. Go to **[railway.app](https://railway.app)** → New Project
2. **Deploy from GitHub repo** → `ahmedanany115-lab/geovany-rts-crm`
3. Set **Root Directory**: `Backend`
4. Railway detects the `Dockerfile` automatically

### Step 2 — Set environment variables

In Railway → your service → **Variables**:

| Variable | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Jwt__SigningKey` | Run `openssl rand -base64 48` and paste result (min 32 chars) |
| `Cors__AllowedOrigins__0` | `https://erp.rtegy.com` |
| `ConnectionStrings__DefaultConnection` | `Host=db.<project>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<pass>;SSL Mode=Require;Trust Server Certificate=true` |

### Step 3 — Set custom domain

Railway → Settings → Domains → Custom Domain → `api.rtegy.com`

Then add a CNAME in your DNS:
```
api  CNAME  <your-app>.railway.app
```

---

## Frontend — Vercel environment variable

In **Vercel** → your project → Settings → Environment Variables:

| Name | Environment | Value |
|---|---|---|
| `NEXT_PUBLIC_API_BASE_URL` | Production, Preview | `https://api.rtegy.com/api/v1` |

Then **Redeploy** in Vercel.

---

## Verify

```bash
# 1. Health check
curl https://api.rtegy.com/health

# 2. Login
curl -X POST https://api.rtegy.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"sara.hassan@rts-erp.demo","password":"Admin@12345!"}'
```

Default admin seeded automatically:
- **Email:** `sara.hassan@rts-erp.demo`
- **Password:** `Admin@12345!`

---

## What Cannot Go on Vercel

Vercel **does not support**:
- .NET / C# runtimes
- Custom Docker images
- Long-running processes

The Next.js frontend stays on Vercel. The .NET API must be on Railway, Render, Azure, or any Docker host.

---

## Local Development

```bash
# Backend — needs PostgreSQL running locally or a Supabase connection
cd Backend
# Set connection string in appsettings.Development.json or user secrets
dotnet run --project src/RTSErp.Api

# Frontend
cd Frontend
cp .env.local.example .env.local
# Edit NEXT_PUBLIC_API_BASE_URL=http://localhost:8080/api/v1
npm install && npm run dev
```
