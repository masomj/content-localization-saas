# Story 6.6 — CLI Integration Spec

This document defines the integration contract for automation clients using the official `content-localization-cli`.

## Transport and auth
- Base URL: configured via `--base-url` or `CLSAAS_BASE_URL`
- Token: configured via `--api-token` or `CLSAAS_API_TOKEN`
- Header: `X-Api-Token: <token>`
- Idempotency hint for export pulls: `X-Request-Id: <guid>`

## Core pull flow
- Endpoint: `GET /api/integration/exports/bundle`
- Required query: `projectId=<guid>`
- Optional query: `language=<bcp47>`, `namespace=<prefix>`
- Success payload includes `signedBundleUrl` (`data:application/json;base64,...`) and `payload`.

## CLI commands
1. `configure --base-url <url> --api-token <token>`
2. `pull --project-id <guid> [--language <code>] [--namespace <ns>] [--out <file>]`

## Exit code contract
- `0` success
- `2` usage/config error
- `10` unauthorized
- `11` forbidden/scope denied
- `12` not found
- `13` rate limited
- `20` server/API error
- `21` network/transport error

## Machine-readable spec endpoint
- Endpoint: `GET /api/cli/spec`
- Auth: Viewer+
- Cache policy: `Cache-Control: no-store`
- Freshness header: `X-Generated-At-Utc`
- Returns CLI version, command list, env vars, and exit-code map.
