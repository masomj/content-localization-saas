# Authentication Architecture

## Overview

The authentication system provides production-parity authentication across all environments (dev, test, prod) using ASP.NET Identity for user management and JWT tokens for stateless authentication.

## Components

### Backend (ASP.NET Core)

#### AuthController (`api/ContentLocalizationSaaS.Api/Controllers/AuthController.cs`)

Provides REST API endpoints for authentication:

| Endpoint | Method | Auth Required | Description |
|----------|--------|---------------|-------------|
| `/api/auth/register` | POST | No | Register new user, auto-creates workspace |
| `/api/auth/login` | POST | No | Authenticate and receive JWT token |
| `/api/auth/me` | GET | Yes | Get current user info |
| `/api/auth/logout` | POST | Yes | Invalidate session (client-side token removal) |
| `/api/auth/refresh` | POST | Yes | Refresh JWT token |

#### JWT Configuration

JWT tokens are configured via `appsettings.json`:

```json
{
  "Auth": {
    "Jwt": {
      "Issuer": "content-localization-api",
      "Audience": "content-localization-clients",
      "SigningKey": "dev-only-change-me-to-32-plus-chars"
    }
  }
}
```

#### Role Assignment Strategy

- **First registered user**: Automatically assigned **Admin** role and workspace owner
- **Subsequent users**: Assigned **Viewer** role by default
- Roles: `Viewer`, `Editor`, `Reviewer`, `Admin`

#### Claims Structure

JWT tokens include the following claims:

| Claim Type | Description |
|------------|-------------|
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier` | User ID |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` | Email |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name` | Display name |
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | Role name |
| `app_role` | App role (Viewer, Editor, etc.) |
| `first_name` | User's first name |
| `last_name` | User's last name |
| `jti` | Unique token ID |

### Frontend (Nuxt.js)

#### useAuth Composable (`frontend/app/composables/useAuth.ts`)

Provides reactive authentication state and methods:

```typescript
const auth = useAuth()

// State
auth.user           // Current user info
auth.organization  // Current workspace
auth.isAuthenticated  // Boolean auth status
auth.isAdmin        // Admin role check
auth.isLoading      // Loading state

// Methods
await auth.login(email, password)
await auth.logout()
await auth.register({ email, password, firstName, lastName, company })
await auth.refreshUser()
```

#### Storage

- `InterCopy_auth_token`: JWT token
- `InterCopy_user`: Serialized user info
- `InterCopy_organization`: Workspace info

#### Session Bootstrap

On app initialization, `useAuth` attempts to:
1. Load stored token and user from localStorage
2. Call `/api/auth/me` to validate token and get current user
3. If validation fails, clear auth state

## Environment Configuration

### Development

```json
{
  "Auth": {
    "Jwt": {
      "SigningKey": "dev-only-change-me-to-32-plus-chars"
    }
  }
}
```

### Production

Generate a secure signing key (minimum 32 characters):
```bash
openssl rand -base64 32
```

Set via environment variable:
```bash
export Auth__Jwt__SigningKey="your-secure-key-here"
```

### Test

Use the same configuration as development. E2E tests create isolated users with unique email suffixes.

## Migration from Mock Auth

The previous mock authentication system has been replaced:

| Old | New |
|-----|-----|
| `mock_token_*` | Real JWT tokens from `/api/auth/login` |
| Email contains "admin" | Real role from backend claims |
| Fallback mode | Removed - real auth required |
| localStorage only | Token validation via `/api/auth/me` |

## API Integration

Frontend makes HTTP requests with Bearer token:

```typescript
const response = await fetch('/api/auth/me', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
})
```

## Security Considerations

1. **Token Expiration**: Tokens expire after 7 days
2. **Password Requirements**: Minimum 8 characters
3. **First-User Admin**: First registered user gets admin (safe for local dev/test)
4. **HTTPS**: Ensure production enforces HTTPS

## Troubleshooting

### "Invalid credentials" on valid login
- Check database connectivity
- Verify user exists in database

### "Demo mode" notice still appearing
- Clear localStorage: `localStorage.clear()`
- Refresh page after authentication

### Role not updating
- Logout and login again to get fresh claims
- Check database for correct role assignment

## Testing

### Unit Tests
```bash
cd frontend && npm run test:unit
```

### E2E Tests
```bash
cd frontend && npm run test:e2e
```

E2E tests automatically create test users with unique suffixes to avoid conflicts.
