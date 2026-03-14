# API Versioning Strategy

## Current version
- **Default version:** `v1`
- Version is represented in API docs and contract notes as `v1`.
- Existing endpoints remain backward-compatible within major `v1`.

## Change policy
- **Non-breaking changes** (new optional fields, new endpoints): stay in `v1`.
- **Breaking changes** (required field changes, response shape breaks, auth contract breaks): introduce `v2` routes/docs and keep `v1` during migration period.

## Deprecation policy
1. Mark endpoint/field as deprecated in docs.
2. Keep compatibility for at least one release cycle.
3. Publish migration guidance before removal.

## Error contract
All error responses use RFC7807 `application/problem+json` shape:
- `type`
- `title`
- `status`
- `detail`
- `instance` (when provided)
- extension fields for domain-specific diagnostics (for example `error`, `guidance`, `actor`).
