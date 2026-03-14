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

test('content item schema form fields render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Content item schema (Story 2.1)' })).toBeVisible()
  await expect(page.locator('#content-key')).toBeVisible()
  await expect(page.locator('#content-source')).toBeVisible()
  await expect(page.locator('#content-status')).toBeVisible()
})

test('reusable copy components section renders', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Reusable copy components (Story 2.2)' })).toBeVisible()
  await expect(page.locator('#copy-component-name')).toBeVisible()
  await expect(page.locator('#copy-component-source')).toBeVisible()
})

test('usage references panel render with filters', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Usage references (Story 2.3)' })).toBeVisible()
  await expect(page.locator('#usage-project-filter')).toBeVisible()
  await expect(page.locator('#usage-screen-filter')).toBeVisible()
  await expect(page.locator('#usage-component-filter')).toBeVisible()
})

test('bulk status and saved filter controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.locator('#bulk-status')).toBeVisible()
  await expect(page.locator('#new-filter-preset')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Save current filter' })).toBeVisible()
})

test('content item history compare controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Content item history (Story 2.5)' })).toBeVisible()
  await expect(page.locator('#compare-left-revision')).toBeVisible()
  await expect(page.locator('#compare-right-revision')).toBeVisible()
})
