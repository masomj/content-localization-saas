# OpenAPI Examples (Story 11.1 addendum)

When viewing `/openapi/v1.json` (development), use the following canonical examples for client integration testing.

## Success example — export bundle
**Operation:** `GET /api/integration/exports/bundle`

```json
{
  "signedBundleUrl": "data:application/json;base64,eyJzY2hlbWEiOiJuZXV0cmFsLnYxIn0=",
  "payload": {
    "schema": "neutral.v1",
    "projectId": "5d6409d0-3af7-442f-86dd-9eb70d3ef96d",
    "language": "fr-FR",
    "files": {
      "common": {
        "cta.save": "Enregistrer"
      }
    }
  }
}
```

## Error example — invalid transition
**Operation:** `POST /api/review-workflow/approve`

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Conflict",
  "status": 409,
  "detail": "Only In Review items can be approved.",
  "error": "invalid_transition",
  "from": "draft",
  "to": "approved",
  "guidance": "Only In Review items can be approved."
}
```

## Error example — stale write
**Operation:** `POST /api/review-workflow/approve|submit|reject`

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Conflict",
  "status": 409,
  "detail": "Content item changed since read; refresh and retry.",
  "error": "stale_write",
  "guidance": "Content item changed since read; refresh and retry."
}
```
