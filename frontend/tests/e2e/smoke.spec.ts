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

test('language management and per-language task controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Language management (Story 3.1)' })).toBeVisible()
  await expect(page.locator('#language-code')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Per-language tasks (Story 3.2)' })).toBeVisible()
  await expect(page.locator('#task-language')).toBeVisible()
})

test('all-languages grid filter and pagination controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'All-languages grid (Story 3.3)' })).toBeVisible()
  await expect(page.locator('#grid-state-filter')).toBeVisible()
  await expect(page.locator('#grid-sort-by')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Prev', exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Next', exact: true })).toBeVisible()
})

test('translation memory suggestion controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('button', { name: 'Check memory suggestion' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Save manual as memory candidate' })).toBeVisible()
  await expect(page.locator('#task-translation')).toBeVisible()
})

test('outdated translation indicators can render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByText('Per-language tasks (Story 3.2)')).toBeVisible()
})

test('discussion thread controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Discussion threads (Story 4.1)' })).toBeVisible()
  await expect(page.locator('#discussion-content-item')).toBeVisible()
  await expect(page.locator('#discussion-body')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Post thread' })).toBeVisible()
})

test('mentions and notifications controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Mentions and notifications (Story 4.2)' })).toBeVisible()
  await expect(page.locator('#notifications-user-email')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Save notification preferences' })).toBeVisible()
})

test('review workflow controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Review workflow (Story 4.3)' })).toBeVisible()
  await expect(page.locator('#review-content-item')).toBeVisible()
  await expect(page.locator('#reviewer-email')).toBeVisible()
  await expect(page.locator('#review-rejection-reason')).toBeVisible()
})

test('external review and activity feed controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'External review links (Story 4.4)' })).toBeVisible()
  await expect(page.locator('#external-review-item')).toBeVisible()
  await expect(page.locator('#external-review-expiry')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Activity feed (Story 4.5)' })).toBeVisible()
  await expect(page.locator('#activity-project')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Export activity feed' })).toBeVisible()
})

test('plugin authentication controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Plugin authentication (Story 5.1)' })).toBeVisible()
  await expect(page.locator('#plugin-user-email')).toBeVisible()
  await expect(page.locator('#plugin-workspace-id')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Connect plugin' })).toBeVisible()
  await expect(page.locator('#plugin-switch-workspace')).toBeVisible()
})

test('figma layer linking controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Figma layer linking (Story 5.2)' })).toBeVisible()
  await expect(page.locator('#layer-link-project')).toBeVisible()
  await expect(page.locator('#layer-link-id')).toBeVisible()
  await expect(page.locator('#layer-duplicate-rule')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Link selected layer' })).toBeVisible()
})

test('plugin sync and diagnostics controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Plugin sync (Story 5.3)' })).toBeVisible()
  await expect(page.locator('#plugin-sync-project')).toBeVisible()
  await expect(page.locator('#plugin-push-layer')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Pull approved text to plugin' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Plugin diagnostics (Story 5.4)' })).toBeVisible()
  await expect(page.locator('#plugin-scan-layers')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Scan plugin issues' })).toBeVisible()
})

test('neutral export controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Neutral i18n export (Story 6.1)' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Generate neutral export' })).toBeVisible()
})

test('plugin status/context details controls render', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Figma layer linking (Story 5.2)' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Plugin item details (Story 5.5)' })).toBeVisible()
  await expect(page.locator('#plugin-target-language')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Load linked item details' })).toBeVisible()
})
