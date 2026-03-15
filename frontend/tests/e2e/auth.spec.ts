import { test, expect, type Page } from '@playwright/test'

async function waitForHydration(page: Page) {
  await page.waitForFunction(() => {
    return (window as any).__VUE__ !== undefined || document.querySelector('[data-hydrated]') !== null
  }).catch(() => {})
}

test.describe('Login Form Validation', () => {
  test('shows validation errors for empty form submission', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.getByRole('button', { name: 'Sign in' }).click()
    
    await expect(page.locator('.field-error').filter({ hasText: 'Email is required' })).toBeVisible({ timeout: 10000 })
    await expect(page.locator('.field-error').filter({ hasText: 'Password is required' })).toBeVisible()
  })

  test('shows validation error for invalid email format', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.getByLabel('Email').fill('invalid-email')
    await page.getByLabel('Password').fill('password123')
    await page.getByRole('button', { name: 'Sign in' }).click()
    
    await expect(page.locator('.field-error').filter({ hasText: 'valid email' })).toBeVisible({ timeout: 10000 })
  })

  test('clears email error when user starts typing', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.getByRole('button', { name: 'Sign in' }).click()
    
    await expect(page.locator('.field-error').filter({ hasText: 'Email is required' })).toBeVisible({ timeout: 10000 })
    
    await page.getByLabel('Email').fill('test')
    await expect(page.locator('.field-error').filter({ hasText: 'Email is required' })).not.toBeVisible()
  })

  test('shows loading state during submission', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.getByLabel('Email').fill('test@example.com')
    await page.getByLabel('Password').fill('password123')
    
    const submitButton = page.locator('button[type="submit"]')
    await submitButton.click()
    
    await expect(submitButton).toBeDisabled({ timeout: 10000 })
  })

  test('shows fallback notice when in demo mode', async ({ page }) => {
    await page.goto('/login')
    await expect(page.locator('.fallback-notice')).toBeVisible()
    await expect(page.locator('.fallback-notice')).toContainText('Demo mode')
  })
})

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
    await page.getByLabel('Password').fill('password123')
    await page.getByRole('button', { name: 'Create account' }).click()
    
    await expect(page.locator('.field-error').filter({ hasText: 'valid email' })).toBeVisible({ timeout: 10000 })
  })

  test('shows validation error for short password', async ({ page }) => {
    await page.goto('/register')
    await page.waitForLoadState('networkidle')
    await page.getByLabel('First name').fill('John')
    await page.getByLabel('Last name').fill('Doe')
    await page.getByLabel('Work email').fill('test@example.com')
    await page.getByLabel('Password').fill('short')
    await page.getByRole('button', { name: 'Create account' }).click()
    
    await expect(page.locator('.field-error').filter({ hasText: 'at least 8 characters' })).toBeVisible({ timeout: 10000 })
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
    await page.getByLabel('Password').fill('password123')
    
    const submitButton = page.locator('button[type="submit"]')
    await submitButton.click()
    
    await expect(submitButton).toBeDisabled({ timeout: 10000 })
  })

  test('shows fallback notice when in demo mode', async ({ page }) => {
    await page.goto('/register')
    await expect(page.locator('.fallback-notice')).toBeVisible()
    await expect(page.locator('.fallback-notice')).toContainText('Demo mode')
  })
})

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

test.describe('Login/Register Navigation', () => {
  test('navigates from login to register', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('link', { name: 'Sign up for free' }).click()
    await expect(page).toHaveURL('/register')
  })

  test('navigates from register to login', async ({ page }) => {
    await page.goto('/register')
    await page.getByRole('link', { name: 'Sign in' }).click()
    await expect(page).toHaveURL('/login')
  })
})
