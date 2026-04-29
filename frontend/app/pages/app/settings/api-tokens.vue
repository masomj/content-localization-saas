<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import UiCard from '~/components/ui/Card.vue'
import UiInput from '~/components/ui/Input.vue'
import { integrationTokensClient } from '~/api/integrationTokensClient'
import type { IntegrationApiToken, IntegrationApiTokenSecret, RotateIntegrationApiTokenSecret } from '~/api/types'

definePageMeta({
  layout: 'app',
  middleware: ['admin'],
})

useSeoMeta({
  title: 'API Tokens - InterCopy',
})

const SUPPORTED_SCOPE = 'exports:read'

const tokens = ref<IntegrationApiToken[]>([])
const isLoading = ref(false)
const isCreating = ref(false)
const isRevoking = ref(false)
const isRotating = ref(false)
const isExtending = ref(false)
const feedback = ref<{ type: 'success' | 'error'; message: string } | null>(null)

const showCreateModal = ref(false)
const showRevokeModal = ref(false)
const showRotateModal = ref(false)
const showExtendModal = ref(false)
const revealedSecret = ref<IntegrationApiTokenSecret | RotateIntegrationApiTokenSecret | null>(null)

const selectedToken = ref<IntegrationApiToken | null>(null)
const createName = ref('')
const createExpiry = ref('')
const createScope = ref(SUPPORTED_SCOPE)
const createError = ref('')
const extendExpiry = ref('')
const extendError = ref('')
const copyState = ref<'idle' | 'copied' | 'error'>('idle')

const activeTokens = computed(() => tokens.value.filter(token => !token.isRevoked))

function showFeedback(type: 'success' | 'error', message: string) {
  feedback.value = { type, message }
  setTimeout(() => {
    if (feedback.value?.message === message) {
      feedback.value = null
    }
  }, 4000)
}

function pad(value: number) {
  return value.toString().padStart(2, '0')
}

function toLocalDateTimeInput(iso: string | null | undefined) {
  if (!iso) return ''
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return ''
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function toDefaultExpiryInput(days = 90) {
  const date = new Date()
  date.setDate(date.getDate() + days)
  date.setHours(17, 0, 0, 0)
  return toLocalDateTimeInput(date.toISOString())
}

function toUtcIso(value: string) {
  return value ? new Date(value).toISOString() : null
}

function formatDateTime(value: string | null | undefined) {
  if (!value) return 'Never'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}

function relativeExpiry(token: IntegrationApiToken) {
  const expiry = new Date(token.expiresUtc).getTime()
  const diffMs = expiry - Date.now()
  if (diffMs <= 0) return 'Expired'
  const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24))
  if (diffDays === 1) return 'Expires in 1 day'
  if (diffDays < 30) return `Expires in ${diffDays} days`
  const diffMonths = Math.round(diffDays / 30)
  return diffMonths <= 1 ? 'Expires in ~1 month' : `Expires in ~${diffMonths} months`
}

async function loadTokens() {
  isLoading.value = true
  try {
    tokens.value = await integrationTokensClient.list()
  } catch (error: any) {
    showFeedback('error', error?.message || 'Failed to load API tokens')
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  createName.value = ''
  createScope.value = SUPPORTED_SCOPE
  createExpiry.value = toDefaultExpiryInput()
  createError.value = ''
  showCreateModal.value = true
}

function openRevokeModal(token: IntegrationApiToken) {
  selectedToken.value = token
  showRevokeModal.value = true
}

function openRotateModal(token: IntegrationApiToken) {
  selectedToken.value = token
  showRotateModal.value = true
}

function openExtendModal(token: IntegrationApiToken) {
  selectedToken.value = token
  extendExpiry.value = toLocalDateTimeInput(token.expiresUtc)
  extendError.value = ''
  showExtendModal.value = true
}

function closeSecretModal() {
  revealedSecret.value = null
  copyState.value = 'idle'
}

async function createToken() {
  createError.value = ''
  if (!createName.value.trim()) {
    createError.value = 'Token name is required.'
    return
  }

  isCreating.value = true
  try {
    revealedSecret.value = await integrationTokensClient.create(
      createName.value.trim(),
      createScope.value,
      toUtcIso(createExpiry.value),
    )
    copyState.value = 'idle'
    showCreateModal.value = false
    await loadTokens()
    showFeedback('success', 'API token created')
  } catch (error: any) {
    createError.value = error?.message || 'Failed to create token'
  } finally {
    isCreating.value = false
  }
}

async function revokeToken() {
  if (!selectedToken.value) return
  isRevoking.value = true
  try {
    await integrationTokensClient.revoke(selectedToken.value.id)
    showRevokeModal.value = false
    showFeedback('success', `Revoked ${selectedToken.value.name}`)
    selectedToken.value = null
    await loadTokens()
  } catch (error: any) {
    showFeedback('error', error?.message || 'Failed to revoke token')
  } finally {
    isRevoking.value = false
  }
}

async function rotateToken() {
  if (!selectedToken.value) return
  isRotating.value = true
  try {
    revealedSecret.value = await integrationTokensClient.rotate(selectedToken.value.id)
    copyState.value = 'idle'
    showRotateModal.value = false
    showFeedback('success', `Rotated ${selectedToken.value.name}`)
    selectedToken.value = null
    await loadTokens()
  } catch (error: any) {
    showFeedback('error', error?.message || 'Failed to rotate token')
  } finally {
    isRotating.value = false
  }
}

async function extendToken() {
  extendError.value = ''
  if (!selectedToken.value) return
  if (!extendExpiry.value) {
    extendError.value = 'Choose a new expiry date.'
    return
  }

  isExtending.value = true
  try {
    await integrationTokensClient.extend(selectedToken.value.id, toUtcIso(extendExpiry.value) || '')
    showExtendModal.value = false
    showFeedback('success', `Extended ${selectedToken.value.name}`)
    selectedToken.value = null
    await loadTokens()
  } catch (error: any) {
    extendError.value = error?.message || 'Failed to extend token expiry'
  } finally {
    isExtending.value = false
  }
}

async function copyTokenValue() {
  if (!revealedSecret.value?.token) return
  try {
    await navigator.clipboard.writeText(revealedSecret.value.token)
    copyState.value = 'copied'
  } catch {
    copyState.value = 'error'
  }
}

onMounted(() => {
  loadTokens()
})
</script>

<template>
  <div class="api-tokens-page">
    <header class="page-header">
      <div>
        <h1>API Tokens</h1>
        <p class="page-subtitle">Create and manage machine credentials for CI/CD export pulls.</p>
      </div>
      <UiButton @click="openCreateModal">
        <svg viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        Create token
      </UiButton>
    </header>

    <div v-if="feedback" :class="['feedback', `feedback--${feedback.type}`]">
      {{ feedback.message }}
    </div>

    <UiCard class="info-card">
      <h2>How CI authenticates</h2>
      <p>
        Use a dedicated token with the <code>exports:read</code> scope. Store the raw token in your CI provider as a
        secret and send it in the <code>X-Api-Token</code> header or via the CLI environment variables.
      </p>
      <ul>
        <li><strong>Recommended secrets:</strong> <code>CLSAAS_BASE_URL</code>, <code>CLSAAS_API_TOKEN</code>, <code>INTERCOPY_PROJECT_ID</code></li>
        <li><strong>One-time visibility:</strong> raw token values are only shown when a token is created or rotated</li>
        <li><strong>Scope today:</strong> export pulls only (<code>exports:read</code>)</li>
      </ul>
    </UiCard>

    <UiCard class="tokens-card">
      <div class="tokens-card__header">
        <div>
          <h2>Existing tokens</h2>
          <p class="tokens-card__subtitle">{{ activeTokens.length }} active token{{ activeTokens.length === 1 ? '' : 's' }}</p>
        </div>
        <UiButton variant="secondary" @click="loadTokens">Refresh</UiButton>
      </div>

      <div v-if="isLoading" class="tokens-state">Loading tokens…</div>
      <div v-else-if="tokens.length === 0" class="tokens-state tokens-state--empty">
        No tokens yet. Create one for your CI pipeline.
      </div>
      <div v-else class="tokens-table-wrap">
        <table class="tokens-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Scope</th>
              <th>Status</th>
              <th>Created</th>
              <th>Last used</th>
              <th>Expiry</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="token in tokens" :key="token.id">
              <td>
                <div class="token-name">{{ token.name }}</div>
                <div class="token-id">{{ token.id }}</div>
              </td>
              <td><code>{{ token.scope }}</code></td>
              <td>
                <span :class="['status-pill', token.isRevoked ? 'status-pill--revoked' : 'status-pill--active']">
                  {{ token.isRevoked ? 'Revoked' : 'Active' }}
                </span>
              </td>
              <td>{{ formatDateTime(token.createdUtc) }}</td>
              <td>{{ formatDateTime(token.lastUsedUtc) }}</td>
              <td>
                <div>{{ formatDateTime(token.expiresUtc) }}</div>
                <div class="token-meta">{{ relativeExpiry(token) }}</div>
              </td>
              <td>
                <div class="token-actions">
                  <UiButton variant="ghost" size="sm" :disabled="token.isRevoked" @click="openExtendModal(token)">Extend</UiButton>
                  <UiButton variant="ghost" size="sm" :disabled="token.isRevoked" @click="openRotateModal(token)">Rotate</UiButton>
                  <UiButton variant="danger" size="sm" :disabled="token.isRevoked" @click="openRevokeModal(token)">Revoke</UiButton>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </UiCard>

    <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
      <div class="modal-card">
        <div class="modal-card__header">
          <div>
            <h2>Create API token</h2>
            <p>Generate a machine credential for CI export pulls.</p>
          </div>
          <button class="modal-close" aria-label="Close" @click="showCreateModal = false">×</button>
        </div>

        <div class="modal-form">
          <div class="form-group">
            <label for="tokenName" class="label-with-hint">
              <span>Name</span>
              <span class="label-hint">Use something traceable, like “sample app GitHub Actions”.</span>
            </label>
            <UiInput id="tokenName" v-model="createName" type="text" />
          </div>

          <div class="form-group">
            <label for="tokenScope" class="label-with-hint">
              <span>Scope</span>
              <span class="label-hint">Only export access is supported right now.</span>
            </label>
            <select id="tokenScope" v-model="createScope" class="form-select">
              <option :value="SUPPORTED_SCOPE">exports:read</option>
            </select>
          </div>

          <div class="form-group">
            <label for="tokenExpiry" class="label-with-hint">
              <span>Expiry</span>
              <span class="label-hint">Tokens expire automatically. Rotate before this date if the pipeline still needs access.</span>
            </label>
            <input id="tokenExpiry" v-model="createExpiry" type="datetime-local" class="form-input" />
          </div>

          <p v-if="createError" class="form-error">{{ createError }}</p>
        </div>

        <div class="modal-actions">
          <UiButton variant="secondary" @click="showCreateModal = false">Cancel</UiButton>
          <UiButton :loading="isCreating" @click="createToken">Create token</UiButton>
        </div>
      </div>
    </div>

    <div v-if="showRevokeModal && selectedToken" class="modal-overlay" @click.self="showRevokeModal = false">
      <div class="modal-card modal-card--narrow">
        <div class="modal-card__header">
          <div>
            <h2>Revoke token</h2>
            <p>This will immediately stop any pipeline using <strong>{{ selectedToken.name }}</strong>.</p>
          </div>
          <button class="modal-close" aria-label="Close" @click="showRevokeModal = false">×</button>
        </div>

        <div class="modal-actions">
          <UiButton variant="secondary" @click="showRevokeModal = false">Cancel</UiButton>
          <UiButton variant="danger" :loading="isRevoking" @click="revokeToken">Revoke token</UiButton>
        </div>
      </div>
    </div>

    <div v-if="showRotateModal && selectedToken" class="modal-overlay" @click.self="showRotateModal = false">
      <div class="modal-card modal-card--narrow">
        <div class="modal-card__header">
          <div>
            <h2>Rotate token</h2>
            <p>
              This revokes <strong>{{ selectedToken.name }}</strong> and creates a replacement token with the same scope.
              Update your CI secret immediately after rotation.
            </p>
          </div>
          <button class="modal-close" aria-label="Close" @click="showRotateModal = false">×</button>
        </div>

        <div class="modal-actions">
          <UiButton variant="secondary" @click="showRotateModal = false">Cancel</UiButton>
          <UiButton :loading="isRotating" @click="rotateToken">Rotate token</UiButton>
        </div>
      </div>
    </div>

    <div v-if="showExtendModal && selectedToken" class="modal-overlay" @click.self="showExtendModal = false">
      <div class="modal-card modal-card--narrow">
        <div class="modal-card__header">
          <div>
            <h2>Extend expiry</h2>
            <p>Choose a later expiry for <strong>{{ selectedToken.name }}</strong>.</p>
          </div>
          <button class="modal-close" aria-label="Close" @click="showExtendModal = false">×</button>
        </div>

        <div class="modal-form">
          <div class="form-group">
            <label for="extendExpiry" class="label-with-hint">
              <span>New expiry</span>
              <span class="label-hint">Must be later than the token’s current expiry.</span>
            </label>
            <input id="extendExpiry" v-model="extendExpiry" type="datetime-local" class="form-input" />
          </div>
          <p v-if="extendError" class="form-error">{{ extendError }}</p>
        </div>

        <div class="modal-actions">
          <UiButton variant="secondary" @click="showExtendModal = false">Cancel</UiButton>
          <UiButton :loading="isExtending" @click="extendToken">Save expiry</UiButton>
        </div>
      </div>
    </div>

    <div v-if="revealedSecret" class="modal-overlay" @click.self="closeSecretModal">
      <div class="modal-card modal-card--wide">
        <div class="modal-card__header">
          <div>
            <h2>Copy this token now</h2>
            <p>This is the only time InterCopy will show the raw token value.</p>
          </div>
          <button class="modal-close" aria-label="Close" @click="closeSecretModal">×</button>
        </div>

        <div class="secret-box">{{ revealedSecret.token }}</div>

        <div class="secret-actions">
          <UiButton @click="copyTokenValue">{{ copyState === 'copied' ? 'Copied' : 'Copy token' }}</UiButton>
          <span v-if="copyState === 'error'" class="form-error">Clipboard copy failed — copy it manually before closing.</span>
        </div>

        <div class="usage-guide">
          <h3>Recommended GitHub Actions secrets</h3>
          <ul>
            <li><code>CLSAAS_BASE_URL</code> → your InterCopy base URL</li>
            <li><code>CLSAAS_API_TOKEN</code> → paste this raw token value</li>
            <li><code>INTERCOPY_PROJECT_ID</code> → the InterCopy project to pull from</li>
          </ul>
          <pre v-pre><code>env:
  CLSAAS_BASE_URL: ${{ secrets.CLSAAS_BASE_URL }}
  CLSAAS_API_TOKEN: ${{ secrets.CLSAAS_API_TOKEN }}</code></pre>
        </div>

        <div class="modal-actions">
          <UiButton @click="closeSecretModal">Done</UiButton>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.api-tokens-page {
  max-width: 1200px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--spacing-4);
  margin-bottom: var(--spacing-6);
}

.page-header h1 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-1) 0;
}

.page-subtitle {
  color: var(--color-text-secondary);
  margin: 0;
}

.btn-icon {
  width: 1.25em;
  height: 1.25em;
}

.feedback {
  padding: var(--spacing-3) var(--spacing-4);
  border-radius: var(--radius-lg);
  margin-bottom: var(--spacing-4);
  font-size: var(--font-size-sm);
}

.feedback--success {
  background: color-mix(in srgb, var(--color-success) 12%, var(--color-surface));
  color: var(--color-text-primary);
  border: 1px solid color-mix(in srgb, var(--color-success) 40%, var(--color-border));
}

.feedback--error {
  background: color-mix(in srgb, var(--color-error) 14%, var(--color-surface));
  color: var(--color-text-primary);
  border: 1px solid color-mix(in srgb, var(--color-error) 45%, var(--color-border));
}

.info-card,
.tokens-card {
  padding: var(--spacing-6);
}

.info-card {
  margin-bottom: var(--spacing-6);
}

.info-card h2,
.tokens-card h2,
.usage-guide h3 {
  margin: 0 0 var(--spacing-2) 0;
  color: var(--color-text-primary);
}

.info-card p,
.info-card ul,
.usage-guide ul {
  margin: 0;
  color: var(--color-text-secondary);
}

.info-card ul,
.usage-guide ul {
  padding-left: var(--spacing-5);
  margin-top: var(--spacing-3);
}

.tokens-card__header {
  display: flex;
  justify-content: space-between;
  gap: var(--spacing-4);
  align-items: flex-start;
  margin-bottom: var(--spacing-5);
}

.tokens-card__subtitle {
  margin: var(--spacing-1) 0 0;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.tokens-state {
  padding: var(--spacing-8);
  text-align: center;
  color: var(--color-text-secondary);
  border: 1px dashed var(--color-border);
  border-radius: var(--radius-lg);
}

.tokens-table-wrap {
  overflow-x: auto;
}

.tokens-table {
  width: 100%;
  border-collapse: collapse;
}

.tokens-table th,
.tokens-table td {
  padding: var(--spacing-3);
  border-bottom: 1px solid var(--color-border);
  text-align: left;
  vertical-align: top;
  font-size: var(--font-size-sm);
}

.tokens-table th {
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-semibold);
}

.token-name {
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.token-id,
.token-meta {
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  margin-top: var(--spacing-1);
}

.token-actions {
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacing-2);
}

.status-pill {
  display: inline-flex;
  align-items: center;
  padding: 0.25rem 0.6rem;
  border-radius: var(--radius-full);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
}

.status-pill--active {
  background: color-mix(in srgb, var(--color-success) 14%, var(--color-surface));
  color: color-mix(in srgb, var(--color-success) 70%, var(--color-text-primary));
}

.status-pill--revoked {
  background: color-mix(in srgb, var(--color-error) 14%, var(--color-surface));
  color: color-mix(in srgb, var(--color-error) 70%, var(--color-text-primary));
}

.modal-overlay {
  position: fixed;
  inset: 0;
  background: color-mix(in srgb, var(--color-background) 45%, transparent);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-4);
  z-index: var(--z-modal-backdrop);
}

.modal-card {
  width: min(100%, 560px);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-xl);
  padding: var(--spacing-6);
  z-index: var(--z-modal);
}

.modal-card--narrow {
  width: min(100%, 480px);
}

.modal-card--wide {
  width: min(100%, 760px);
}

.modal-card__header {
  display: flex;
  justify-content: space-between;
  gap: var(--spacing-4);
  margin-bottom: var(--spacing-5);
}

.modal-card__header h2 {
  margin: 0 0 var(--spacing-1) 0;
  color: var(--color-text-primary);
}

.modal-card__header p {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.modal-close {
  border: none;
  background: transparent;
  color: var(--color-text-muted);
  font-size: 1.75rem;
  line-height: 1;
  cursor: pointer;
}

.modal-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.label-with-hint {
  display: flex;
  flex-direction: column;
  gap: 2px;
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
}

.label-hint {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-normal);
  color: var(--color-text-muted);
}

.form-input,
.form-select {
  width: 100%;
  padding: var(--spacing-3) var(--spacing-4);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-surface);
  color: var(--color-text-primary);
}

.form-error {
  color: var(--color-error);
  font-size: var(--font-size-sm);
  margin: 0;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-3);
  margin-top: var(--spacing-5);
}

.secret-box {
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-4);
  font-family: monospace;
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
  word-break: break-all;
}

.secret-actions {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  margin-top: var(--spacing-4);
}

.usage-guide {
  margin-top: var(--spacing-5);
  padding: var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: color-mix(in srgb, var(--color-primary-50) 30%, var(--color-surface));
}

.usage-guide pre {
  margin: var(--spacing-4) 0 0;
  padding: var(--spacing-4);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  border: 1px solid var(--color-border);
  overflow-x: auto;
}

@media (max-width: 768px) {
  .page-header,
  .tokens-card__header,
  .modal-actions,
  .secret-actions {
    flex-direction: column;
    align-items: stretch;
  }

  .tokens-table th:nth-child(4),
  .tokens-table td:nth-child(4),
  .tokens-table th:nth-child(5),
  .tokens-table td:nth-child(5) {
    display: none;
  }
}
</style>
