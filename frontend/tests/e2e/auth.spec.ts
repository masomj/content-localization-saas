import { test, expect } from '@playwright/test'

// ---------------------------------------------------------------------------
// Login page – now an OIDC redirect (Keycloak). No local email/password form.
// ---------------------------------------------------------------------------
test.describe('Login Page (OIDC redirect)', () => {
  test('renders the redirect card', async ({ page }) => {
    await page.goto('/login')
    await expect(page.locator('.auth-card')).toBeVisible({ timeout: 15000 })
  })

  test('has a Continue with Keycloak button', async ({ page }) => {
    await page.goto('/login')
    const btn = page.getByRole('button', { name: 'Continue with Keycloak' })
    await expect(btn).toBeVisible({ timeout: 15000 })
  })
})

// ---------------------------------------------------------------------------
// Register page – still has a full local form
// ---------------------------------------------------------------------------
test.describe('Register Form Validation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/register')
    await expect(page.locator('.auth-card')).toBeVisible({ timeout: 15000 })
  })

  test('shows validation errors for empty form submission', async ({ page }) => {
    await page.getByRole('button', { name: 'Create account' }).click()

    await expect(page.locator('.field-error').filter({ hasText: 'First name is required' })).toBeVisible({ timeout: 10000 })
    await expect(page.locator('.field-error').filter({ hasText: 'Last name is required' })).toBeVisible()
    await expect(page.locator('.field-error').filter({ hasText: 'email' })).toBeVisible()
    await expect(page.locator('.field-error').filter({ hasText: 'Password' })).toBeVisible()
  })

  test('shows validation error for invalid email format', async ({ page }) => {
    await page.locator('#firstName').fill('John')
    await page.locator('#lastName').fill('Doe')
    await page.locator('#email').fill('invalid-email')
    await page.locator('#password').fill('password123')
    await page.locator('#confirmPassword').fill('password123')
    await page.getByRole('button', { name: 'Create account' }).click()

    await expect(page.locator('.field-error').filter({ hasText: 'email' })).toBeVisible({ timeout: 10000 })
  })

  test('shows validation error for short password', async ({ page }) => {
    await page.locator('#firstName').fill('John')
    await page.locator('#lastName').fill('Doe')
    await page.locator('#email').fill('test@example.com')
    await page.locator('#password').fill('short')
    await page.locator('#confirmPassword').fill('short')
    await page.getByRole('button', { name: 'Create account' }).click()

    await expect(page.locator('.field-error').filter({ hasText: 'at least 8' })).toBeVisible({ timeout: 10000 })
  })

  test('shows validation error when passwords do not match', async ({ page }) => {
    await page.locator('#firstName').fill('John')
    await page.locator('#lastName').fill('Doe')
    await page.locator('#email').fill('test@example.com')
    await page.locator('#password').fill('password123')
    await page.locator('#confirmPassword').fill('different456')
    await page.getByRole('button', { name: 'Create account' }).click()

    await expect(page.locator('.field-error').filter({ hasText: 'match' })).toBeVisible({ timeout: 10000 })
  })
})

// ---------------------------------------------------------------------------
// Public route access
// ---------------------------------------------------------------------------
test.describe('Public Route Access', () => {
  test('landing page is accessible', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('.landing-page')).toBeVisible({ timeout: 15000 })
  })

  test('login page is accessible', async ({ page }) => {
    await page.goto('/login')
    await expect(page.locator('.auth-card')).toBeVisible({ timeout: 15000 })
  })

  test('register page is accessible', async ({ page }) => {
    await page.goto('/register')
    await expect(page.locator('.auth-card')).toBeVisible({ timeout: 15000 })
  })
})

// ---------------------------------------------------------------------------
// Navigation between auth pages
// ---------------------------------------------------------------------------
test.describe('Auth Page Navigation', () => {
  test('navigates from register to login', async ({ page }) => {
    await page.goto('/register')
    await expect(page.locator('.auth-card')).toBeVisible({ timeout: 15000 })
    await page.getByRole('link', { name: 'Sign in' }).click()
    await expect(page).toHaveURL('/login')
  })
})
