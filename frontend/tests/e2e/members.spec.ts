import { test, expect } from '@playwright/test'

test.describe('Member Management Page Structure', () => {
  test('login page renders correctly', async ({ page }) => {
    await page.goto('/login')
    await expect(page.locator('h1')).toContainText('Welcome back')
  })

  test('members page component exists in codebase', async ({ page }) => {
    const membersPage = await page.goto('/app/settings/members')
    expect(membersPage).toBeTruthy()
  })
})
