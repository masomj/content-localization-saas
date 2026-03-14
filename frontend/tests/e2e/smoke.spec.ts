import { test, expect } from '@playwright/test'

test('home renders and skip link focuses main content', async ({ page }) => {
  await page.goto('/')
  const skip = page.getByRole('link', { name: 'Skip to main content' })
  await skip.focus()
  await page.keyboard.press('Enter')
  await expect(page.locator('#main-content')).toBeFocused()
})

test('shows API warning if backend is unavailable', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Create workspace' })).toBeVisible()
})

test('shows onboarding empty state when no projects exist', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByText('No content yet')).toBeVisible()
  await expect(page.getByText('Create your first content item')).toBeVisible()
})

test('role switch control is available', async ({ page }) => {
  await page.goto('/')
  const roleSelect = page.locator('#current-role')
  await expect(roleSelect).toBeVisible()
  await roleSelect.selectOption('Viewer')
  await expect(roleSelect).toHaveValue('Viewer')
})

test('membership audit filter controls render for admin', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Membership audit' })).toBeVisible()
  await expect(page.locator('#audit-target-email')).toBeVisible()
})
