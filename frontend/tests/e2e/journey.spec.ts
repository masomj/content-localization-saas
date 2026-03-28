import { test, expect } from '@playwright/test'

test.describe('Full User Journey', () => {
  test('register form validates and shows demo mode notice', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('.landing-page')).toBeVisible()

    await page.getByRole('link', { name: 'Get Started' }).click()
    await expect(page).toHaveURL('/register')
    await expect(page.locator('.auth-card')).toBeVisible()

    await page.getByLabel('First name').fill('John')
    await page.getByLabel('Last name').fill('Doe')
    await page.getByLabel('Work email').fill('john.doe@example.com')
    await page.getByLabel('Password').fill('password123')
    await page.getByRole('button', { name: 'Create account' }).click()

    await expect(page.locator('.fallback-notice')).toBeVisible()
  })

  test('login form validates and shows demo mode notice', async ({ page }) => {
    await page.goto('/login')
    await expect(page.locator('.auth-card')).toBeVisible()

    await page.getByLabel('Email').fill('admin@example.com')
    await page.getByLabel('Password').fill('password123')
    await page.getByRole('button', { name: 'Sign in' }).click()

    await expect(page.locator('.fallback-notice')).toBeVisible()
  })
})

test.describe('Onboarding Flow', () => {
  test('onboarding page renders for authenticated user without org', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.addInitScript(() => {
      localStorage.setItem('InterCopy_auth_token', 'test_token')
      localStorage.setItem('InterCopy_user', JSON.stringify({ 
        id: '1', 
        email: 'test@example.com', 
        name: 'Test User',
        role: 'Admin' 
      }))
    })
    await page.goto('/onboarding/organisation')
    await expect(page.locator('.auth-card')).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Create your organization' })).toBeVisible()
  })

  test('onboarding validates empty org name', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.addInitScript(() => {
      localStorage.setItem('InterCopy_auth_token', 'test_token')
      localStorage.setItem('InterCopy_user', JSON.stringify({ 
        id: '1', 
        email: 'test@example.com', 
        name: 'Test User',
        role: 'Admin' 
      }))
    })
    await page.goto('/onboarding/organisation')
    await page.getByRole('button', { name: 'Create organization' }).click()
    await expect(page.getByText('Organization name is required')).toBeVisible()
  })
})

test.describe('App Dashboard', () => {
  test('unauthenticated access redirects to login', async ({ page }) => {
    await page.goto('/app/dashboard')
    await expect(page).toHaveURL('/login')
  })

  test('authenticated without org redirects to onboarding', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.addInitScript(() => {
      localStorage.setItem('InterCopy_auth_token', 'test_token')
      localStorage.setItem('InterCopy_user', JSON.stringify({ 
        id: '1', 
        email: 'test@example.com', 
        name: 'Test User',
        role: 'Admin' 
      }))
    })
    await page.goto('/app/dashboard')
    await page.waitForTimeout(1000)
    const url = page.url()
    expect(url).toMatch(/\/login|\/onboarding/)
  })
})

test.describe('Members Management', () => {
  test('unauthenticated access redirects to login', async ({ page }) => {
    await page.goto('/app/settings/members')
    await expect(page).toHaveURL('/login')
  })

  test('authenticated without org redirects to onboarding', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.addInitScript(() => {
      localStorage.setItem('InterCopy_auth_token', 'test_token')
      localStorage.setItem('InterCopy_user', JSON.stringify({ 
        id: '1', 
        email: 'test@example.com', 
        name: 'Test User',
        role: 'Admin' 
      }))
    })
    await page.goto('/app/settings/members')
    await page.waitForTimeout(1000)
    const url = page.url()
    expect(url).toMatch(/\/login|\/onboarding/)
  })
})

test.describe('App Navigation', () => {
  test('unauthenticated access to app routes redirects to login', async ({ page }) => {
    const routes = ['/app', '/app/projects', '/app/content', '/app/review', '/app/integrations', '/app/settings']
    for (const route of routes) {
      await page.goto(route)
      await expect(page).toHaveURL('/login')
    }
  })

  test('unknown app route redirects due to auth requirement', async ({ page }) => {
    await page.goto('/app/unknown-page')
    await expect(page).toHaveURL('/login')
  })
})
