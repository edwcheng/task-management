# Railway Deployment Guide

This document summarizes the lessons learned from deploying the Task Management application to Railway.

## Architecture

- **Frontend**: Vue.js 3 + TypeScript + Vite, served by nginx
- **Backend**: .NET Core 8.0 + Entity Framework Core
- **Database**: PostgreSQL

## Key Lessons Learned

### 1. Railway Uses Dynamic PORT Assignment

**Problem**: Hardcoding port 80 in nginx config caused `connection refused` errors.

**Solution**: Railway assigns a dynamic `PORT` environment variable. Your application must read this variable and listen on that port.

```dockerfile
# Dockerfile
ENV PORT=8080
EXPOSE 8080
CMD ["/start.sh"]
```

```nginx
# nginx.conf.template
server {
    listen ${PORT};
    ...
}
```

```bash
# start.sh - substitute PORT at runtime
envsubst '${PORT}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf
exec nginx -g 'daemon off;'
```

### 2. Nginx Requires MIME Types for JavaScript Modules

**Problem**: JavaScript files were served as `text/plain`, causing browsers to reject ES modules with error:
```
Expected a JavaScript-or-Wasm module script but the server responded with a MIME type of "text/plain"
```

**Solution**: Include MIME types configuration in nginx:

```nginx
http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;
    ...
}
```

### 3. Railway Service Discovery

**Problem**: How do services communicate with each other?

**Solution**: Use Railway's internal DNS:
- Service name: `backend`
- Internal domain: `backend.railway.internal`
- URL: `http://backend.railway.internal:5000`

### 4. Backend Must Use Fixed Port for Internal Communication

**Problem**: Railway may override the `PORT` variable for external traffic, but internal service-to-service communication needs a consistent port.

**Solution**: Hardcode the backend to listen on port 5000 for internal networking:

```csharp
// Program.cs
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
});
```

### 5. Database Auto-Creation on Railway

**Problem**: PostgreSQL database doesn't exist on first deployment.

**Solution**: Auto-create database on startup:

```csharp
// Connect to 'postgres' database first
var builder2 = new NpgsqlConnectionStringBuilder(connectionString);
var dbName = builder2.Database;
builder2.Database = "postgres"; // Default database

using var connection = new NpgsqlConnection(builder2.ToString());
await connection.OpenAsync();

// Check and create database
var cmd = connection.CreateCommand();
cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
var exists = await cmd.ExecuteScalarAsync();

if (exists == null)
{
    cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
    await cmd.ExecuteNonQueryAsync();
}

// Then use EF Core to create tables
await dbContext.Database.EnsureCreatedAsync();
```

### 6. Don't Override CMD in railway.toml

**Problem**: Setting `startCommand` in `railway.toml` bypasses the Dockerfile's entrypoint/CMD.

**Incorrect**:
```toml
[deploy]
startCommand = "nginx -g 'daemon off;'"  # Bypasses startup script!
```

**Correct**:
```toml
[deploy]
restartPolicyType = "ON_FAILURE"
restartPolicyMaxRetries = 10
# Let Dockerfile's CMD handle startup
```

### 7. Install Required Tools in Docker Image

**Problem**: `envsubst` command not found in nginx:alpine image.

**Solution**: Install gettext package:

```dockerfile
FROM nginx:alpine AS final
RUN apk add --no-cache gettext
```

## Complete Working Configuration

### Frontend Dockerfile.railway

```dockerfile
# Build stage
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
ARG VITE_API_URL=/api
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# Runtime stage
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
RUN apk add --no-cache gettext
COPY --from=build /app/dist .
COPY nginx.conf.template /etc/nginx/nginx.conf.template
COPY start.sh /start.sh
RUN chmod +x /start.sh
ENV PORT=8080
EXPOSE 8080
CMD ["/start.sh"]
```

### nginx.conf.template

```nginx
events {
    worker_connections 1024;
}

http {
    include       /etc/nginx/mime.types;
    default_type  application/octet-stream;
    sendfile        on;
    keepalive_timeout  65;

    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;

    server {
        listen ${PORT};
        root /usr/share/nginx/html;
        index index.html;

        location / {
            try_files $uri $uri/ /index.html;
        }

        location /api {
            proxy_pass http://backend.railway.internal:5000;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
    }
}
```

### start.sh

```bash
#!/bin/sh
set -e

echo "Starting nginx on port ${PORT}"
envsubst '${PORT}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf
exec nginx -g 'daemon off;'
```

### Backend Program.cs (Port Configuration)

```csharp
// Always use port 5000 for Railway private networking
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
});
```

## Railway Environment Variables

### Frontend Service
- `PORT`: Assigned by Railway (don't set manually)
- `VITE_API_URL`: `/api` (relative path for nginx proxy)

### Backend Service
- `PORT`: Don't override - let it use 5000
- `DefaultConnection`: PostgreSQL connection string (from Railway database reference)
- `Jwt:SecretKey`: Your JWT secret key

## Troubleshooting

### 502 Bad Gateway with "connection refused"
- Check if nginx is starting correctly (view deployment logs)
- Ensure `PORT` environment variable is being used
- Check if `start.sh` has execute permissions

### JavaScript MIME Type Errors
- Add `include /etc/nginx/mime.types;` to nginx config

### Frontend Can't Reach Backend
- Use Railway's internal DNS: `backend.railway.internal`
- Ensure backend is listening on port 5000
- Check if both services are in the same Railway project

### Database Connection Errors
- Verify PostgreSQL service is connected to backend
- Check connection string format
- Ensure database auto-creation logic is working
