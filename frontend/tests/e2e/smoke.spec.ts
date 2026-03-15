import { test, expect } from '@playwright/test'

test('skip link focuses main content on /', async ({ page }) => {
  await page.goto('/')
  const skip = page.getByRole('link', { name: 'Skip to main content' })
  await skip.focus()
  await page.keyboard.press('Enter')
  await expect(page.locator('#main-content')).toBeFocused()
})

test('landing page shows nav, sections, and CTA', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('link', { name: 'Log in' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Get Started' })).toBeVisible()
  const mainNav = page.locator('nav[aria-label="Main navigation"]')
  await expect(mainNav.getByRole('link', { name: 'Features' })).toBeVisible()
  await expect(mainNav.getByRole('link', { name: 'Benefits' })).toBeVisible()
  await expect(mainNav.getByRole('link', { name: 'Pricing' })).toBeVisible()
})

test('nav CTA links navigate to /login and /register', async ({ page }) => {
  await page.goto('/')
  await page.getByRole('link', { name: 'Log in' }).click()
  await expect(page).toHaveURL('/login')
  await page.goto('/')
  await page.getByRole('link', { name: 'Get Started' }).click()
  await expect(page).toHaveURL('/register')
})

test('/login has heading and email/password + submit', async ({ page }) => {
  await page.goto('/login')
  await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible()
  await expect(page.getByLabel('Email')).toBeVisible()
  await expect(page.getByLabel('Password')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible()
})

test('/register has heading and basic form controls + submit', async ({ page }) => {
  await page.goto('/register')
  await expect(page.getByRole('heading', { name: 'Create your account' })).toBeVisible()
  await expect(page.getByLabel('First name')).toBeVisible()
  await expect(page.getByLabel('Last name')).toBeVisible()
  await expect(page.getByLabel('Work email')).toBeVisible()
  await expect(page.getByLabel('Password')).toBeVisible()
  await expect(page.getByLabel('Company name')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Create account' })).toBeVisible()
})

test('form inputs have proper labels and ids', async ({ page }) => {
  await page.goto('/login')
  await page.waitForLoadState('networkidle')
  const emailInput = page.getByLabel('Email')
  await expect(emailInput).toHaveAttribute('id')
  const emailLabel = page.locator('label[for="' + await emailInput.getAttribute('id') + '"]')
  await expect(emailLabel).toBeVisible()
})

test.describe('App Shell Navigation', () => {
  test.skip('pages render with correct headings - requires SSR auth fix', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.addInitScript(() => {
      localStorage.setItem('auth_token', 'test_token')
      localStorage.setItem('auth_user', JSON.stringify({ id: '1', email: 'admin@example.com', name: 'Admin', role: 'Admin' }))
      localStorage.setItem('auth_organization', JSON.stringify({ id: 'org_1', name: 'Test Org' }))
    })
    await page.reload()
    await page.waitForLoadState('networkidle')
    
    await page.goto('/app/dashboard')
    await expect(page.getByRole('heading', { name: 'Welcome to LocFlow' })).toBeVisible()
    
    await page.goto('/app/projects')
    await expect(page.getByRole('heading', { name: 'Projects' })).toBeVisible()
    
    await page.goto('/app/content')
    await expect(page.getByRole('heading', { name: 'Content' })).toBeVisible()
    
    await page.goto('/app/review')
    await expect(page.getByRole('heading', { name: 'Review' })).toBeVisible()
    
    await page.goto('/app/integrations')
    await expect(page.getByRole('heading', { name: 'Integrations' })).toBeVisible()
    
    await page.goto('/app/settings')
    await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible()
  })

  test.skip('breadcrumbs render correctly - requires SSR auth fix', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.addInitScript(() => {
      localStorage.setItem('auth_token', 'test_token')
      localStorage.setItem('auth_user', JSON.stringify({ id: '1', email: 'admin@example.com', name: 'Admin', role: 'Admin' }))
      localStorage.setItem('auth_organization', JSON.stringify({ id: 'org_1', name: 'Test Org' }))
    })
    await page.reload()
    await page.waitForLoadState('networkidle')
    
    await page.goto('/app/projects')
    const breadcrumbs = page.locator('.breadcrumbs')
    await expect(breadcrumbs).toBeVisible()
    await expect(breadcrumbs.getByText('Home')).toBeVisible()
    await expect(breadcrumbs.getByText('Projects')).toBeVisible()
  })

  test.skip('app 404 page renders for unknown routes - requires SSR auth fix', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.addInitScript(() => {
      localStorage.setItem('auth_token', 'test_token')
      localStorage.setItem('auth_user', JSON.stringify({ id: '1', email: 'admin@example.com', name: 'Admin', role: 'Admin' }))
      localStorage.setItem('auth_organization', JSON.stringify({ id: 'org_1', name: 'Test Org' }))
    })
    await page.reload()
    await page.waitForLoadState('networkidle')
    
    await page.goto('/app/unknown-page')
    await expect(page.getByRole('heading', { name: 'Page Not Found' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Go to Dashboard' })).toBeVisible()
  })

  test.skip('sidebar navigation links work - requires SSR auth fix', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    await page.addInitScript(() => {
      localStorage.setItem('auth_token', 'test_token')
      localStorage.setItem('auth_user', JSON.stringify({ id: '1', email: 'admin@example.com', name: 'Admin', role: 'Admin' }))
      localStorage.setItem('auth_organization', JSON.stringify({ id: 'org_1', name: 'Test Org' }))
    })
    await page.reload()
    await page.waitForLoadState('networkidle')
    
    await page.goto('/app/dashboard')
    
    await page.getByRole('link', { name: 'Projects' }).click()
    await expect(page).toHaveURL('/app/projects')
    
    await page.getByRole('link', { name: 'Content' }).click()
    await expect(page).toHaveURL('/app/content')
    
    await page.getByRole('link', { name: 'Review' }).click()
    await expect(page).toHaveURL('/app/review')
    
    await page.getByRole('link', { name: 'Integrations' }).click()
    await expect(page).toHaveURL('/app/integrations')
    
    await page.getByRole('link', { name: 'Settings' }).click()
    await expect(page).toHaveURL('/app/settings')
  })
})
