<script setup lang="ts">
const props = defineProps<{ apiBase: string }>()

const filters = reactive({ targetEmail: '', action: '', fromUtc: '', toUtc: '' })
const rows = ref<any[]>([])
const error = ref('')

async function loadAudit() {
  error.value = ''
  try {
    const params = new URLSearchParams()
    if (filters.targetEmail.trim()) params.set('targetEmail', filters.targetEmail.trim())
    if (filters.action.trim()) params.set('action', filters.action.trim())
    if (filters.fromUtc.trim()) params.set('fromUtc', filters.fromUtc.trim())
    if (filters.toUtc.trim()) params.set('toUtc', filters.toUtc.trim())

    const qs = params.toString()
    rows.value = await $fetch(`${props.apiBase}/api/admin/membership-audit${qs ? `?${qs}` : ''}`, {
      headers: { 'X-User-Role': 'Admin' },
    })
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

      <label for="audit-action">Action</label>
      <input id="audit-action" v-model="filters.action" placeholder="invite_created" />

      <label for="audit-from">From (UTC)</label>
      <input id="audit-from" v-model="filters.fromUtc" placeholder="2026-03-14T00:00:00Z" />

      <label for="audit-to">To (UTC)</label>
      <input id="audit-to" v-model="filters.toUtc" placeholder="2026-03-15T00:00:00Z" />

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
