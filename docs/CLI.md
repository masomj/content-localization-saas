# Content Localization CLI (v1)

Official production CLI for export/pull automation.

## Build

```bash
cd api
 dotnet build ContentLocalizationSaaS.Cli/ContentLocalizationSaaS.Cli.csproj -c Release
```

## Usage

```bash
# Save non-interactive config
 dotnet run --project api/ContentLocalizationSaaS.Cli -- configure \
  --base-url http://localhost:5177 \
  --api-token <token>

# Pull/export content bundle
 dotnet run --project api/ContentLocalizationSaaS.Cli -- pull \
  --project-id <guid> \
  --language fr-FR \
  --out exports/fr-FR.json
```

Environment-only mode (CI-safe):

```bash
export CLSAAS_BASE_URL=http://localhost:5177
export CLSAAS_API_TOKEN=<token>
dotnet run --project api/ContentLocalizationSaaS.Cli -- pull --project-id <guid>
```

## Exit codes

- `0` success
- `2` usage/config error
- `10` unauthorized (401)
- `11` forbidden/scope denied (403)
- `12` not found (404)
- `13` rate-limited (429)
- `20` API/server error (5xx or parse failure)
- `21` network/transport failure
