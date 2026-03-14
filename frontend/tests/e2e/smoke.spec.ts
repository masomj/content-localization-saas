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
