<script setup lang="ts">
import { adminClient } from '~/api/adminClient'
import type { MembershipAuditRow } from '~/api/types'

const props = defineProps<{ apiBase: string }>()

const filters = reactive({ targetEmail: '', action: '', fromUtc: '', toUtc: '' })
const rows = ref<MembershipAuditRow[]>([])
const error = ref('')

async function loadAudit() {
  error.value = ''
  try {
    rows.value = await adminClient.membershipAudit(filters)
  } catch {
    error.value = 'Could not load membership audit logs.'
  }
}

function exportCsvLink() {
  const params = new URLSearchParams()
  params.set('format', 'csv')
  if (filters.targetEmail.trim()) params.set('targetEmail', filters.targetEmail.trim())
  if (filters.action.trim()) params.set('action', filters.action.trim())
  if (filters.fromUtc.trim()) params.set('fromUtc', filters.fromUtc.trim())
  if (filters.toUtc.trim()) params.set('toUtc', filters.toUtc.trim())
  return `${props.apiBase}/api/admin/membership-audit?${params.toString()}`
}

onMounted(loadAudit)
</script>

<template>
  <section class="card" aria-label="Membership audit logs">
    <h2>Membership audit</h2>
    <form @submit.prevent="loadAudit" novalidate>
      <label for="audit-target-email">Target email</label>
      <input id="audit-target-email" v-model="filters.targetEmail" />

      <label for="audit-action" class="label-with-hint">
        <span>Action</span>
        <span class="label-hint">invite_created</span>
      </label>
      <input id="audit-action" v-model="filters.action" />

      <label for="audit-from" class="label-with-hint">
        <span>From (UTC)</span>
        <span class="label-hint">2026-03-14T00:00:00Z</span>
      </label>
      <input id="audit-from" v-model="filters.fromUtc" />

      <label for="audit-to" class="label-with-hint">
        <span>To (UTC)</span>
        <span class="label-hint">2026-03-15T00:00:00Z</span>
      </label>
      <input id="audit-to" v-model="filters.toUtc" />

      <button type="submit">Filter audit logs</button>
      <a :href="exportCsvLink()">Export CSV</a>
    </form>

    <p v-if="error" class="error">{{ error }}</p>

    <ul>
      <li v-for="row in rows" :key="row.id">
        {{ row.createdUtc }} · {{ row.action }} · {{ row.targetEmail }} ({{ row.oldValue }} -> {{ row.newValue }})
      </li>
    </ul>

    <p v-if="rows.length === 0">No membership audit rows for current filters.</p>
  </section>
</template>

<style scoped>
.label-with-hint {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.label-hint {
  font-size: 0.75rem;
  font-weight: 400;
  color: var(--color-gray-500);
}
</style>
