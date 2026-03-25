import { test, expect } from '@playwright/test'

// ---------------------------------------------------------------------------
// Login page – now an OIDC redirect (Keycloak). No local email/password form.
// ---------------------------------------------------------------------------
test.describe('Login Page (OIDC redirect)', () => {
  test('renders the redirect card', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')

    await expect(page.locator('.auth-card')).toBeVisible()
    await expect(page.locator('h1')).toHaveText('Redirecting to sign in…')
  })

  test('has a Continue with Keycloak button', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')

    const btn = page.getByRole('button', { name: 'Continue with Keycloak' })
    await expect(btn).toBeVisible()
  })
})

// ---------------------------------------------------------------------------
// Register page – still has a full local form
// ---------------------------------------------------------------------------
test.describe('Register Form Validation', () => {
  test('shows validation errors for empty form submission', async ({ page }) => {
    await page.goto('/register')
    await page.waitForLoadState('networkidle')
    await page.getByRole('button', { name: 'Create account' }).click()

    await expect(page.locator('.field-error').filter({ hasText: 'First name is required' })).toBeVisible({ timeout: 10000 })
    await expect(page.locator('.field-error').filter({ hasText: 'Last name is required' })).toBeVisible()
    await expect(page.locator('.field-error').filter({ hasText: 'Work email is required' })).toBeVisible()
    await expect(page.locator('.field-error').filter({ hasText: 'Password is required' })).toBeVisible()
  })

  test('shows validation error for invalid email format', async ({ page }) => {
    await page.goto('/register')
    await page.waitForLoadState('networkidle')
    await page.getByLabel('First name').fill('John')
    await page.getByLabel('Last name').fill('Doe')
    await page.getByLabel('Work email').fill('invalid-email')
    await page.locator('#password').fill('password123')
    await page.locator('#confirmPassword').fill('password123')
    await page.getByRole('button', { name: 'Create account' }).click()

    await expect(page.locator('.field-error').filter({ hasText: 'valid email' })).toBeVisible({ timeout: 10000 })
  })

  test('shows validation error for short password', async ({ page }) => {
    await page.goto('/register')
    await page.waitForLoadState('networkidle')
    await page.getByLabel('First name').fill('John')
    await page.getByLabel('Last name').fill('Doe')
    await page.getByLabel('Work email').fill('test@example.com')
    await page.locator('#password').fill('short')
    await page.locator('#confirmPassword').fill('short')
    await page.getByRole('button', { name: 'Create account' }).click()

    await expect(page.locator('.field-error').filter({ hasText: 'at least 8 characters' })).toBeVisible({ timeout: 10000 })
  })

  test('shows validation error when passwords do not match', async ({ page }) => {
    await page.goto('/register')
    await page.waitForLoadState('networkidle')
    await page.getByLabel('First name').fill('John')
    await page.getByLabel('Last name').fill('Doe')
    await page.getByLabel('Work email').fill('test@example.com')
    await page.locator('#password').fill('password123')
    await page.locator('#confirmPassword').fill('different456')
    await page.getByRole('button', { name: 'Create account' }).click()

    await expect(page.locator('.field-error').filter({ hasText: 'Passwords do not match' })).toBeVisible({ timeout: 10000 })
  })

  test('clears field errors when user starts typing', async ({ page }) => {
    await page.goto('/register')
    await page.waitForLoadState('networkidle')
    await page.getByRole('button', { name: 'Create account' }).click()

    await expect(page.locator('.field-error').filter({ hasText: 'First name is required' })).toBeVisible({ timeout: 10000 })

    await page.getByLabel('First name').fill('J')
    await expect(page.locator('.field-error').filter({ hasText: 'First name is required' })).not.toBeVisible()
  })

  test('shows loading state during submission', async ({ page }) => {
    await page.goto('/register')
    await page.waitForLoadState('networkidle')
    await page.getByLabel('First name').fill('John')
    await page.getByLabel('Last name').fill('Doe')
    await page.getByLabel('Work email').fill('test@example.com')
    await page.locator('#password').fill('password123')
    await page.locator('#confirmPassword').fill('password123')

    const submitButton = page.locator('button[type="submit"]')
    await submitButton.click()

    await expect(submitButton).toBeDisabled({ timeout: 10000 })
  })
})

// ---------------------------------------------------------------------------
// Public route access & redirects
// ---------------------------------------------------------------------------
test.describe('Protected Route Redirect', () => {
  test('redirects unauthenticated user from /app to /login', async ({ page }) => {
    await page.goto('/app')
    await expect(page).toHaveURL('/login')
  })

  test('redirects unauthenticated user from /app/anything to /login', async ({ page }) => {
    await page.goto('/app/some/nested/route')
    await expect(page).toHaveURL('/login')
  })

  test('allows access to public routes when not authenticated', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('.landing-page')).toBeVisible()

    await page.goto('/login')
    await expect(page.locator('.auth-card')).toBeVisible()

    await page.goto('/register')
    await expect(page.locator('.auth-card')).toBeVisible()
  })
})

// ---------------------------------------------------------------------------
// Navigation between auth pages
// ---------------------------------------------------------------------------
test.describe('Auth Page Navigation', () => {
  test('navigates from register to login', async ({ page }) => {
    await page.goto('/register')
    await page.getByRole('link', { name: 'Sign in' }).click()
    await expect(page).toHaveURL('/login')
  })
})
