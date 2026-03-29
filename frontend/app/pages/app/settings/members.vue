<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import InviteMemberModal from '~/components/InviteMemberModal.vue'
import MembersTable from '~/components/MembersTable.vue'

definePageMeta({
  layout: 'app',
  middleware: ['admin'],
})

useSeoMeta({
  title: 'Team Members - InterCopy',
})

const auth = useAuth()
const router = useRouter()
const { 
  members, 
  invites, 
  isLoading, 
  roleOptions,
  fetchMembers, 
  fetchInvites,
  resendInvite,
  revokeInvite,
  changeRole,
  removeMember 
} = useMembers()

const isModalOpen = ref(false)
const actionFeedback = ref<{ type: 'success' | 'error'; message: string } | null>(null)

const workspaceId = computed(() => {
  const org = auth.organization.value
  return org?.id || ''
})

onMounted(async () => {
  if (!auth.organization.value) {
    router.push('/onboarding/organisation')
    return
  }
  await Promise.all([
    fetchMembers(),
    fetchInvites()
  ])
})

function showFeedback(type: 'success' | 'error', message: string) {
  actionFeedback.value = { type, message }
  setTimeout(() => {
    actionFeedback.value = null
  }, 3000)
}

async function handleResendInvite(email: string, role: string) {
  const result = await resendInvite(workspaceId.value, email, role)
  if (result.success) {
    showFeedback('success', `Invite resent to ${email}`)
  } else {
    showFeedback('error', result.error || 'Failed to resend invite')
  }
}

async function handleRevokeInvite(email: string) {
  const result = await revokeInvite(workspaceId.value, email)
  if (result.success) {
    showFeedback('success', `Invite revoked for ${email}`)
    await fetchMembers()
  } else {
    showFeedback('error', result.error || 'Failed to revoke invite')
  }
}

async function handleChangeRole(email: string, role: string) {
  const result = await changeRole(workspaceId.value, email, role)
  if (result.success) {
    showFeedback('success', `Role changed for ${email}`)
    await fetchMembers()
  } else {
    showFeedback('error', result.error || 'Failed to change role')
  }
}

async function handleRemoveMember(email: string) {
  if (!confirm(`Are you sure you want to remove ${email} from the team?`)) return
  
  const result = await removeMember(workspaceId.value, email)
  if (result.success) {
    showFeedback('success', `${email} has been removed`)
    await fetchMembers()
  } else {
    showFeedback('error', result.error || 'Failed to remove member')
  }
}

function handleInvited() {
  showFeedback('success', 'Invitation sent successfully')
  fetchMembers()
}
</script>

<template>
  <div class="members-page">
    <header class="members-page__header">
      <div>
        <h1>Team Members</h1>
        <p class="members-page__subtitle">Manage your team and their access to the workspace</p>
      </div>
      <UiButton @click="isModalOpen = true">
        <svg viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        Invite Member
      </UiButton>
    </header>

    <div v-if="actionFeedback" :class="['feedback', `feedback--${actionFeedback.type}`]">
      {{ actionFeedback.message }}
    </div>

    <MembersTable
      :members="members"
      :invites="invites"
      :role-options="roleOptions"
      :is-loading="isLoading"
      :current-user-email="auth.user.value?.email || ''"
      @resend-invite="handleResendInvite"
      @revoke-invite="handleRevokeInvite"
      @change-role="handleChangeRole"
      @remove-member="handleRemoveMember"
    />

    <InviteMemberModal
      :is-open="isModalOpen"
      :workspace-id="workspaceId"
      :role-options="roleOptions"
      @close="isModalOpen = false"
      @invited="handleInvited"
    />
  </div>
</template>

<style scoped>
.members-page {
  max-width: 1000px;
}

.members-page__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-6);
}

.members-page__header h1 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-1) 0;
}

.members-page__subtitle {
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
</style>

