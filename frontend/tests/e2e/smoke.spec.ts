import { test, expect } from '@playwright/test'

test('home renders and skip link focuses main content', async ({ page }) => {
  await page.goto('/')
  const skip = page.getByRole('link', { name: 'Skip to main content' })
  await skip.focus()
  await page.keyboard.press('Enter')
  await expect(page.locator('#main-content')).toBeFocused()
})

test('shows API offline warning in local dev when backend is not started', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByText('API is not reachable yet. Start the backend to persist data.')).toBeVisible()
})

test('shows onboarding empty state when no projects exist', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByText('No content yet')).toBeVisible()
  await expect(page.getByText('Create your first content item')).toBeVisible()
})
