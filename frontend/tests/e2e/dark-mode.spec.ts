import { test, expect } from '@playwright/test'

async function setDarkMode(page: any) {
  await page.addInitScript(() => {
    localStorage.setItem('locflow-theme', 'dark')
  })
}

test.describe('Dark mode regression checks', () => {
  test('public/auth routes render dark surfaces and toggle', async ({ page }) => {
    await setDarkMode(page)

    for (const route of ['/', '/login', '/register', '/onboarding/organisation']) {
      await page.goto(route)
      await expect(page.locator('[data-testid="theme-toggle"]').first()).toBeVisible()
      const bg = await page.evaluate(() => getComputedStyle(document.body).backgroundColor)
      expect(bg).not.toBe('rgb(255, 255, 255)')
    }
  })

})
