# Railway Deployment Guide

This guide walks you through deploying the Task Manager application to Railway.

## Prerequisites

1. A [Railway](https://railway.app) account
2. GitHub account (for easy deployment)

## Architecture on Railway

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Frontend      │────▶│    Backend      │────▶│   PostgreSQL    │
│   (Nginx)       │     │   (.NET 8)      │     │   (Database)    │
│   Port 80       │     │   Port 5000     │     │   Port 5432     │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## Deployment Steps

### Step 1: Push to GitHub

1. Create a new GitHub repository
2. Push the project code:
   ```bash
   cd task-management
   git init
   git add .
   git commit -m "Initial commit"
   git branch -M main
   git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO.git
   git push -u origin main
   ```

### Step 2: Create Railway Project

1. Go to [Railway](https://railway.app) and sign in
2. Click **"New Project"**
3. Select **"Deploy from GitHub repo"**
4. Choose your repository

### Step 3: Add PostgreSQL Database

1. In your Railway project, click **"+ New Service"**
2. Select **"Database"**
3. Choose **"PostgreSQL"**
4. Railway will automatically create the database and set environment variables

### Step 4: Deploy Backend

1. Click **"+ New Service"**
2. Select **"GitHub Repo"**
3. Choose your repository
4. Set the **Root Directory** to `backend`
5. Add environment variables:
   ```
   ConnectionStrings__DefaultConnection=Host=${{Postgres.PUBLICDOMAIN}};Database=taskmanager;Username=${{Postgres.USERNAME}};Password=${{Postgres.PASSWORD}}
   Jwt__SecretKey=YourProductionSecretKeyHereChangeThis12345678!
   Jwt__Issuer=TaskManagerAPI
   Jwt__Audience=TaskManagerAPI
   Jwt__ExpirationMinutes=1440
   ASPNETCORE_URLS=http://+:5000
   ```
6. Deploy!

### Step 5: Deploy Frontend

1. Click **"+ New Service"**
2. Select **"GitHub Repo"**
3. Choose your repository
4. Set the **Root Directory** to `frontend`
5. Add environment variable:
   ```
   BACKEND_URL=http://${{backend.RAILWAY_PRIVATE_DOMAIN}}:5000
   ```
6. Deploy!

### Step 6: Configure Networking

1. Go to your **Frontend** service settings
2. Under **Networking**, enable **"Public Domain"**
3. Railway will give you a URL like `https://your-app.up.railway.app`

4. For the **Backend**, you may also want to enable public access for API testing:
   - Go to Backend service settings
   - Enable **"Public Domain"**

## Environment Variables Reference

### Backend
| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=postgres.railway.internal;Database=taskmanager;Username=postgres;Password=xxx` |
| `Jwt__SecretKey` | JWT signing key | `YourSecretKey123...` |
| `Jwt__Issuer` | JWT issuer | `TaskManagerAPI` |
| `Jwt__Audience` | JWT audience | `TaskManagerAPI` |
| `Jwt__ExpirationMinutes` | Token expiry | `1440` |

### Frontend
| Variable | Description | Example |
|----------|-------------|---------|
| `BACKEND_URL` | Backend internal URL | `http://backend.railway.internal:5000` |

## Railway Service References

Railway provides special variables for service references:

- `${{Postgres.PUBLICDOMAIN}}` - PostgreSQL public domain
- `${{Postgres.USERNAME}}` - PostgreSQL username
- `${{Postgres.PASSWORD}}` - PostgreSQL password
- `${{backend.RAILWAY_PRIVATE_DOMAIN}}` - Backend private domain

## Troubleshooting

### Backend won't start
- Check environment variables are set correctly
- Verify PostgreSQL service is running first
- Check logs in Railway dashboard

### Frontend can't reach backend
- Ensure `BACKEND_URL` is set correctly
- Use Railway's private domain: `http://backend.railway.internal:5000`
- Check both services are in the same project

### Database connection errors
- Wait for PostgreSQL to fully initialize (1-2 minutes)
- Check the connection string format
- Verify the database name matches

## Costs

Railway offers:
- **Free tier**: $5/month credit (enough for small apps)
- **Pro tier**: $20/month for more resources

The Task Manager app fits within the free tier for personal use.

## Alternative: Single Service Deployment

If you want to deploy everything in one service:

1. Use the `docker-compose.yml` from the root
2. Railway will deploy it as a single service
3. Note: This may consume more resources

## Need Help?

- [Railway Documentation](https://docs.railway.app)
- [Railway Discord](https://discord.gg/railway)
