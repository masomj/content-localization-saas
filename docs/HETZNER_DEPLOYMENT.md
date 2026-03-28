# InterCopy — Hetzner Deployment Guide

## Architecture Overview

```
                    ┌─────────────┐
                    │   Caddy      │ :80/:443 (auto TLS)
                    │  (reverse    │
                    │   proxy)     │
                    └──┬───┬───┬──┘
                       │   │   │
            ┌──────────┘   │   └──────────┐
            ▼              ▼              ▼
      ┌──────────┐  ┌──────────┐  ┌──────────┐
      │  Nuxt    │  │  .NET    │  │ Keycloak │
      │ Frontend │  │   API    │  │  (auth)  │
      │  :3000   │  │  :5135   │  │  :8080   │
      └──────────┘  └──────────┘  └──────────┘
                         │              │
                    ┌────┴────┐    ┌────┴────┐
                    │contentdb│    │keycloakdb│
                    └────┬────┘    └────┬────┘
                         │              │
                    ┌────┴──────────────┴────┐
                    │   Hetzner Managed       │
                    │   PostgreSQL             │
                    │   (or self-hosted)       │
                    └─────────────────────────┘
```

## Prerequisites

- Hetzner Cloud account
- Domain name (e.g., `InterCopy.app`)
- GitHub repo: `masomj/content-localization-saas`

## 1. Provision Hetzner Server

### Via Hetzner CLI (hcloud)

```bash
# Install hcloud CLI
# https://github.com/hetznercloud/cli

# Create SSH key (if you haven't)
ssh-keygen -t ed25519 -C "InterCopy-deploy"

# Add key to Hetzner
hcloud ssh-key create --name InterCopy --public-key-from-file ~/.ssh/id_ed25519.pub

# Create dev server (CX11 — €3.29/mo)
hcloud server create \
  --name InterCopy-dev \
  --type cx11 \
  --image ubuntu-24.04 \
  --ssh-key InterCopy \
  --location fsn1

# Create prod server (CX22 — €5.39/mo) — do this later when ready
# hcloud server create \
#   --name InterCopy-prod \
#   --type cx22 \
#   --image ubuntu-24.04 \
#   --ssh-key InterCopy \
#   --location fsn1
```

### DNS

Point your domain to the server IP:
```
A    InterCopy.app          → <server-ip>
A    api.InterCopy.app      → <server-ip>
A    auth.InterCopy.app     → <server-ip>
```

## 2. Server Setup Script

SSH in and run:

```bash
#!/bin/bash
set -euo pipefail

# Update system
apt-get update && apt-get upgrade -y

# Install Docker
curl -fsSL https://get.docker.com | sh
systemctl enable docker

# Install Docker Compose plugin
apt-get install -y docker-compose-plugin

# Create app directory
mkdir -p /opt/InterCopy
cd /opt/InterCopy

# Create .env file (fill in your values)
cat > .env << 'EOF'
# Domain
DOMAIN=InterCopy.app
API_DOMAIN=api.InterCopy.app
AUTH_DOMAIN=auth.InterCopy.app

# PostgreSQL (Hetzner Managed or self-hosted)
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_USER=InterCopy
POSTGRES_PASSWORD=CHANGE_ME_STRONG_PASSWORD
CONTENT_DB=content_localization
KEYCLOAK_DB=keycloak

# Keycloak
KEYCLOAK_ADMIN=admin
KEYCLOAK_ADMIN_PASSWORD=CHANGE_ME_ADMIN_PASSWORD

# .NET API
ASPNETCORE_ENVIRONMENT=Production

# Nuxt
NUXT_PUBLIC_API_BASE=/api
EOF

echo "Server setup complete. Edit /opt/InterCopy/.env with your values."
```

## 3. Dockerfiles

### API Dockerfile (`api/Dockerfile`)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY ContentLocalizationSaaS.slnx ./
COPY ContentLocalizationSaaS.Api/ContentLocalizationSaaS.Api.csproj ContentLocalizationSaaS.Api/
COPY ContentLocalizationSaaS.Application/ContentLocalizationSaaS.Application.csproj ContentLocalizationSaaS.Application/
COPY ContentLocalizationSaaS.Domain/ContentLocalizationSaaS.Domain.csproj ContentLocalizationSaaS.Domain/
COPY ContentLocalizationSaaS.Infrastructure/ContentLocalizationSaaS.Infrastructure.csproj ContentLocalizationSaaS.Infrastructure/
COPY ContentLocalizationSaaS.ServiceDefaults/ContentLocalizationSaaS.ServiceDefaults.csproj ContentLocalizationSaaS.ServiceDefaults/

RUN dotnet restore ContentLocalizationSaaS.Api/ContentLocalizationSaaS.Api.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish ContentLocalizationSaaS.Api/ContentLocalizationSaaS.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5135
EXPOSE 5135

ENTRYPOINT ["dotnet", "ContentLocalizationSaaS.Api.dll"]
```

### Frontend Dockerfile (`frontend/Dockerfile`)

```dockerfile
FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .
RUN npm run build

FROM node:24-alpine AS runtime
WORKDIR /app

COPY --from=build /app/.output .output

ENV NITRO_PORT=3000
ENV NITRO_HOST=0.0.0.0
EXPOSE 3000

CMD ["node", ".output/server/index.mjs"]
```

## 4. Docker Compose (`/opt/InterCopy/docker-compose.yml`)

```yaml
services:
  # ── Reverse Proxy ──────────────────────────────

  caddy:
    image: caddy:2-alpine
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
      - "443:443/udp"  # HTTP/3
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile:ro
      - caddy_data:/data
      - caddy_config:/config
    depends_on:
      - frontend
      - api
      - keycloak

  # ── PostgreSQL (self-hosted; swap for managed later) ──
  postgres:
    image: postgres:17-alpine
    restart: unless-stopped
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init-databases.sql:/docker-entrypoint-initdb.d/init.sql:ro
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5

  # ── Keycloak ───────────────────────────────────
  keycloak:
    image: quay.io/keycloak/keycloak:26.1
    restart: unless-stopped
    command: start --optimized --import-realm
    environment:
      KEYCLOAK_ADMIN: ${KEYCLOAK_ADMIN}
      KEYCLOAK_ADMIN_PASSWORD: ${KEYCLOAK_ADMIN_PASSWORD}
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://postgres:5432/${KEYCLOAK_DB}
      KC_DB_USERNAME: ${POSTGRES_USER}
      KC_DB_PASSWORD: ${POSTGRES_PASSWORD}
      KC_HEALTH_ENABLED: "true"
      KC_METRICS_ENABLED: "true"
      KC_HOSTNAME: ${AUTH_DOMAIN}
      KC_PROXY_HEADERS: xforwarded
      KC_HTTP_ENABLED: "true"
    volumes:
      - ./keycloak/realm:/opt/keycloak/data/import:ro
      - ./keycloak/themes:/opt/keycloak/themes:ro
    depends_on:
      postgres:
        condition: service_healthy

  # ── .NET API ───────────────────────────────────
  api:
    build:
      context: ./src/api
      dockerfile: Dockerfile
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}
      ConnectionStrings__contentdb: "Host=postgres;Port=5432;Database=${CONTENT_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
      Auth__Oidc__Issuer: "https://${AUTH_DOMAIN}/realms/InterCopy"
      Auth__Oidc__Audience: "InterCopy-web"
      Auth__Oidc__RequireHttpsMetadata: "true"
    depends_on:
      postgres:
        condition: service_healthy
      keycloak:
        condition: service_started

  # ── Nuxt Frontend ─────────────────────────────
  frontend:
    build:
      context: ./src/frontend
      dockerfile: Dockerfile
    restart: unless-stopped
    environment:
      NUXT_PUBLIC_KEYCLOAK_URL: "https://${AUTH_DOMAIN}"
      NUXT_PUBLIC_KEYCLOAK_REALM: "InterCopy"
      NUXT_PUBLIC_KEYCLOAK_CLIENT_ID: "InterCopy-web"
    depends_on:
      - api

volumes:
  postgres_data:
  caddy_data:
  caddy_config:
```

## 5. Caddyfile (`/opt/InterCopy/Caddyfile`)

```caddyfile
# Frontend
{$DOMAIN} {
    reverse_proxy frontend:3000
}

# API — proxy /api requests
api.{$DOMAIN} {
    reverse_proxy api:5135
}

# Keycloak auth
auth.{$DOMAIN} {
    reverse_proxy keycloak:8080
}
```

Caddy handles TLS certificates automatically via Let's Encrypt. Zero config.

## 6. Database Init Script (`/opt/InterCopy/init-databases.sql`)

```sql
-- Create both databases on first run
SELECT 'CREATE DATABASE content_localization'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'content_localization')\gexec

SELECT 'CREATE DATABASE keycloak'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'keycloak')\gexec
```

## 7. GitHub Actions CI/CD (`.github/workflows/deploy.yml`)

```yaml
name: Deploy to Hetzner

on:
  push:
    branches: [main]
  workflow_dispatch:

env:
  SERVER_HOST: ${{ secrets.HETZNER_HOST }}
  SERVER_USER: root
  DEPLOY_PATH: /opt/InterCopy

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Copy source to server
        uses: appleboy/scp-action@v0.1.7
        with:
          host: ${{ env.SERVER_HOST }}
          username: ${{ env.SERVER_USER }}
          key: ${{ secrets.HETZNER_SSH_KEY }}
          source: "api/,frontend/,keycloak/"
          target: "${{ env.DEPLOY_PATH }}/src/"
          overwrite: true

      - name: Deploy
        uses: appleboy/ssh-action@v1.0.3
        with:
          host: ${{ env.SERVER_HOST }}
          username: ${{ env.SERVER_USER }}
          key: ${{ secrets.HETZNER_SSH_KEY }}
          script: |
            cd ${{ env.DEPLOY_PATH }}
            docker compose build --no-cache
            docker compose up -d --remove-orphans
            docker system prune -f
            echo "Deployed at $(date)"
```

### GitHub Secrets to set:
- `HETZNER_HOST` — your server IP
- `HETZNER_SSH_KEY` — private key for deploy

## 8. Backup Script (`/opt/InterCopy/backup.sh`)

```bash
#!/bin/bash
set -euo pipefail

BACKUP_DIR="/opt/InterCopy/backups"
DATE=$(date +%Y-%m-%d_%H%M)
mkdir -p "$BACKUP_DIR"

# Dump both databases
docker compose exec -T postgres pg_dump -U InterCopy content_localization | gzip > "$BACKUP_DIR/contentdb_$DATE.sql.gz"
docker compose exec -T postgres pg_dump -U InterCopy keycloak | gzip > "$BACKUP_DIR/keycloakdb_$DATE.sql.gz"

# Keep last 14 days
find "$BACKUP_DIR" -name "*.sql.gz" -mtime +14 -delete

echo "Backup complete: $DATE"
```

Add to cron:
```bash
crontab -e
# Daily at 3am
0 3 * * * /opt/InterCopy/backup.sh >> /var/log/InterCopy-backup.log 2>&1
```

## 9. Deployment Checklist

1. [ ] Provision Hetzner CX11 server
2. [ ] Point DNS (InterCopy.app, api.InterCopy.app, auth.InterCopy.app)
3. [ ] SSH in, run server setup script
4. [ ] Copy Dockerfiles to repo (`api/Dockerfile`, `frontend/Dockerfile`)
5. [ ] Copy `docker-compose.yml`, `Caddyfile`, `init-databases.sql` to server
6. [ ] Copy keycloak realm export + themes to server
7. [ ] Edit `.env` with real passwords
8. [ ] `docker compose up -d`
9. [ ] Verify: `curl https://InterCopy.app`, `curl https://api.InterCopy.app/healthz`
10. [ ] Set up GitHub Actions secrets + deploy workflow
11. [ ] Set up backup cron
12. [ ] Test full auth flow (Keycloak → Nuxt → API)

## Upgrade Path

When you need more power:
```bash
# Via Hetzner CLI
hcloud server change-type InterCopy-dev cx22 --keep-disk

# Or via console: Server → Rescale → pick new tier
# ~30 seconds, no data loss
```

When you want managed Postgres:
- Create Hetzner Managed PostgreSQL instance
- Update `.env` with managed host/credentials
- Remove `postgres` service from docker-compose.yml
- `docker compose up -d`
