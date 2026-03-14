# API Contract Examples (v1)

## Success example
`POST /api/workspaces`

```json
{
  "id": "5d6409d0-3af7-442f-86dd-9eb70d3ef96d",
  "name": "Acme Workspace",
  "createdUtc": "2026-03-14T17:00:00Z"
}
```

## Error example (ProblemDetails)
`POST /api/review-workflow/approve` with invalid transition

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

## Validation error example

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "Name": ["Name is required"]
  }
}
```
