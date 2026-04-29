<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import UiCard from '~/components/ui/Card.vue'
import { governanceClient } from '~/api/governanceClient'
import type { GovernanceDashboard } from '~/api/types'

definePageMeta({ layout: 'app', middleware: ['admin', 'feature-flags'] })

const auth = useAuth()

const dashboard = ref<GovernanceDashboard | null>(null)
const isLoading = ref(false)
const loadError = ref('')

async function loadDashboard() {
  const wsId = auth.currentOrganization.value?.id
  if (!wsId) return
  isLoading.value = true
  loadError.value = ''
  try {
    dashboard.value = await governanceClient.getDashboard(wsId)
  } catch (e: any) {
    loadError.value = e?.message || 'Failed to load dashboard'
  } finally {
    isLoading.value = false
  }
}

async function exportCsv() {
  const wsId = auth.currentOrganization.value?.id
  if (!wsId) return
  try {
    const blob = await governanceClient.exportCsv(wsId)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'governance-report.csv'
    a.click()
    URL.revokeObjectURL(url)
  } catch {
    // silent fail for CSV export
  }
}

function maxBar(terms: { term: string; adoptionRate: number }[]) {
  if (!terms.length) return 100
  return Math.max(...terms.map(t => t.adoptionRate), 1)
}

onMounted(() => {
  loadDashboard()
})
</script>

<template>
  <div class="gov">
    <div class="gov__header">
      <div>
        <h1 class="gov__title">Governance Dashboard</h1>
        <p class="gov__desc">Monitor glossary adoption, style compliance, and forbidden term incidents.</p>
      </div>
      <div class="gov__header-actions">
        <UiButton variant="secondary" :disabled="isLoading" @click="loadDashboard">Refresh</UiButton>
        <UiButton variant="secondary" :disabled="!dashboard" @click="exportCsv">Export CSV</UiButton>
      </div>
    </div>

    <div v-if="isLoading" class="gov__loading">Loading dashboard data...</div>
    <p v-else-if="loadError" class="gov__error">{{ loadError }}</p>

    <template v-if="dashboard && !isLoading">
      <!-- Metric cards -->
      <div class="gov__metrics">
        <div class="gov__metric-card">
          <span class="gov__metric-label">Glossary Adoption</span>
          <span class="gov__metric-value">{{ dashboard.glossaryAdoptionRate }}%</span>
        </div>
        <div class="gov__metric-card">
          <span class="gov__metric-label">Style Compliance</span>
          <span class="gov__metric-value">{{ dashboard.styleRuleComplianceRate }}%</span>
        </div>
        <div class="gov__metric-card">
          <span class="gov__metric-label">Forbidden Incidents</span>
          <span class="gov__metric-value gov__metric-value--alert" :class="{ 'gov__metric-value--ok': dashboard.forbiddenTermIncidentCount === 0 }">
            {{ dashboard.forbiddenTermIncidentCount }}
          </span>
        </div>
      </div>

      <!-- Top non-adopted terms -->
      <div class="gov__section">
        <h2 class="gov__section-title">Top Non-Adopted Glossary Terms</h2>
        <div v-if="dashboard.topNonAdoptedTerms.length === 0" class="gov__empty">No glossary terms to display.</div>
        <div v-else class="gov__bars">
          <div v-for="t in dashboard.topNonAdoptedTerms" :key="t.term" class="gov__bar-row">
            <span class="gov__bar-label">{{ t.term }}</span>
            <div class="gov__bar-track">
              <div
                class="gov__bar-fill"
                :style="{ width: (t.adoptionRate / maxBar(dashboard.topNonAdoptedTerms)) * 100 + '%' }"
              />
            </div>
            <span class="gov__bar-value">{{ t.adoptionRate }}%</span>
          </div>
        </div>
      </div>

      <!-- Recent forbidden incidents -->
      <div class="gov__section">
        <h2 class="gov__section-title">Recent Forbidden Term Incidents</h2>
        <div v-if="dashboard.recentForbiddenIncidents.length === 0" class="gov__empty">No incidents found.</div>
        <table v-else class="gov__table">
          <thead>
            <tr>
              <th>Content Key</th>
              <th>Language</th>
              <th>Term</th>
              <th>Translator</th>
              <th>Date</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(inc, i) in dashboard.recentForbiddenIncidents" :key="i">
              <td>
                <NuxtLink :to="`/app/content`" class="gov__link">{{ inc.contentItemKey }}</NuxtLink>
              </td>
              <td>{{ inc.language }}</td>
              <td>{{ inc.term || '—' }}</td>
              <td>{{ inc.translatorEmail || '—' }}</td>
              <td>{{ new Date(inc.date).toLocaleDateString() }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>

<style scoped>
.gov {
  max-width: 960px;
  margin: 0 auto;
  padding: var(--spacing-6);
}

.gov__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-8);
  gap: var(--spacing-4);
  flex-wrap: wrap;
}

.gov__header-actions {
  display: flex;
  gap: var(--spacing-2);
}

.gov__title {
  margin: 0;
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text-primary);
}

.gov__desc {
  margin: var(--spacing-2) 0 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.gov__loading {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  text-align: center;
  padding: var(--spacing-12);
}

.gov__error {
  color: var(--color-error);
  font-size: var(--font-size-sm);
}

.gov__metrics {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: var(--spacing-4);
  margin-bottom: var(--spacing-8);
}

.gov__metric-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-6);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.gov__metric-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-weight: var(--font-weight-medium);
}

.gov__metric-value {
  font-size: var(--font-size-3xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-primary-600);
}

.gov__metric-value--alert {
  color: var(--color-error);
}

.gov__metric-value--ok {
  color: var(--color-success);
}

.gov__section {
  margin-bottom: var(--spacing-8);
}

.gov__section-title {
  margin: 0 0 var(--spacing-4);
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.gov__empty {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  padding: var(--spacing-4) 0;
}

.gov__bars {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}

.gov__bar-row {
  display: grid;
  grid-template-columns: 160px 1fr 60px;
  align-items: center;
  gap: var(--spacing-3);
}

.gov__bar-label {
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.gov__bar-track {
  height: 20px;
  background: var(--color-border);
  border-radius: var(--radius-sm);
  overflow: hidden;
}

.gov__bar-fill {
  height: 100%;
  background: var(--color-primary-500);
  border-radius: var(--radius-sm);
  min-width: 2px;
  transition: width var(--transition-normal);
}

.gov__bar-value {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  text-align: right;
}

.gov__table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}

.gov__table th {
  text-align: left;
  padding: var(--spacing-3) var(--spacing-4);
  border-bottom: 2px solid var(--color-border);
  color: var(--color-text-muted);
  font-weight: var(--font-weight-medium);
  font-size: var(--font-size-xs);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.gov__table td {
  padding: var(--spacing-3) var(--spacing-4);
  border-bottom: 1px solid var(--color-border);
  color: var(--color-text-primary);
}

.gov__table tr:hover td {
  background: color-mix(in srgb, var(--color-primary-50) 30%, transparent);
}

.gov__link {
  color: var(--color-primary-600);
  text-decoration: none;
}

.gov__link:hover {
  text-decoration: underline;
}
</style>
