<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import type { Member, Invite } from '~/composables/useMembers'

const props = defineProps<{
  members: Member[]
  invites: Invite[]
  roleOptions: { value: string; label: string }[]
  isLoading: boolean
  currentUserEmail: string
}>()

const emit = defineEmits<{
  resendInvite: [email: string, role: string]
  revokeInvite: [email: string]
  changeRole: [email: string, role: string]
  removeMember: [email: string]
}>()

const expandedRow = ref<string | null>(null)
const editingRole = ref<string | null>(null)
const selectedRole = ref('')
const actionLoading = ref<string | null>(null)

function toggleRow(email: string) {
  expandedRow.value = expandedRow.value === email ? null : email
}

function startEditRole(email: string, currentRole: string) {
  editingRole.value = email
  selectedRole.value = currentRole
}

function cancelEditRole() {
  editingRole.value = null
  selectedRole.value = ''
}

function saveRole(email: string) {
  if (selectedRole.value) {
    emit('changeRole', email, selectedRole.value)
  }
  cancelEditRole()
}

function getStatus(member: Member): 'Active' | 'Inactive' {
  return member.isActive ? 'Active' : 'Inactive'
}

function getPendingInvite(email: string): Invite | undefined {
  return props.invites.find(i => i.email === email && i.status === 'Pending')
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}
</script>

<template>
  <div class="members-table-wrapper">
    <table class="members-table">
      <thead>
        <tr>
          <th>Email</th>
          <th>Role</th>
          <th>Status</th>
          <th>Joined</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <template v-if="isLoading">
          <tr>
            <td colspan="5" class="loading-cell">Loading...</td>
          </tr>
        </template>
        <template v-else-if="members.length === 0">
          <tr>
            <td colspan="5" class="empty-cell">No members found</td>
          </tr>
        </template>
        <template v-else>
          <tr v-for="member in members" :key="member.id" :class="{ 'row-expanded': expandedRow === member.email }">
            <td class="cell-email">
              <button class="expand-btn" @click="toggleRow(member.email)" :aria-expanded="expandedRow === member.email">
                <svg :class="{ 'rotated': expandedRow === member.email }" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
                </svg>
              </button>
              <span>{{ member.email }}</span>
              <span v-if="member.email === currentUserEmail" class="you-badge">You</span>
            </td>
            <td class="cell-role">
              <template v-if="editingRole === member.email">
                <select v-model="selectedRole" class="role-select" @keyup.enter="saveRole(member.email)" @keyup.escape="cancelEditRole">
                  <option v-for="opt in roleOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                </select>
                <button class="icon-btn success" @click="saveRole(member.email)" title="Save">
                  <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" /></svg>
                </button>
                <button class="icon-btn" @click="cancelEditRole" title="Cancel">
                  <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
                </button>
              </template>
              <template v-else>
                {{ member.role }}
              </template>
            </td>
            <td>
              <span :class="['status-badge', `status-${getStatus(member).toLowerCase()}`]">
                {{ getStatus(member) }}
              </span>
            </td>
            <td class="cell-date">{{ formatDate(member.createdUtc) }}</td>
            <td class="cell-actions">
              <template v-if="expandedRow === member.email">
                <div class="actions-menu" @click.stop>
                  <UiButton 
                    v-if="getPendingInvite(member.email)" 
                    variant="ghost" 
                    size="sm"
                    :loading="actionLoading === `resend-${member.email}`"
                    @click="emit('resendInvite', member.email, member.role)"
                  >
                    Resend Invite
                  </UiButton>
                  <UiButton 
                    v-if="getPendingInvite(member.email)" 
                    variant="ghost" 
                    size="sm"
                    :loading="actionLoading === `revoke-${member.email}`"
                    @click="emit('revokeInvite', member.email)"
                  >
                    Revoke Invite
                  </UiButton>
                  <UiButton 
                    v-if="!getPendingInvite(member.email) && member.email !== currentUserEmail" 
                    variant="ghost" 
                    size="sm"
                    @click="startEditRole(member.email, member.role)"
                  >
                    Change Role
                  </UiButton>
                  <UiButton 
                    v-if="member.email !== currentUserEmail" 
                    variant="danger" 
                    size="sm"
                    :loading="actionLoading === `remove-${member.email}`"
                    @click="emit('removeMember', member.email)"
                  >
                    Remove Member
                  </UiButton>
                </div>
              </template>
            </td>
          </tr>
        </template>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.members-table-wrapper {
  overflow-x: auto;
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-lg);
}

.members-table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}

.members-table th {
  text-align: left;
  padding: var(--spacing-3) var(--spacing-4);
  background: var(--color-surface);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
  border-bottom: 1px solid var(--color-border);
}

.members-table td {
  padding: var(--spacing-3) var(--spacing-4);
  border-bottom: 1px solid var(--color-gray-100);
  vertical-align: middle;
}

.members-table tr:last-child td {
  border-bottom: none;
}

.members-table tr:hover {
  background: var(--color-gray-50);
}

.row-expanded {
  background: var(--color-gray-50);
}

.loading-cell,
.empty-cell {
  text-align: center;
  padding: var(--spacing-8) !important;
  color: var(--color-gray-500);
}

.cell-email {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
}

.expand-btn {
  background: none;
  border: none;
  padding: var(--spacing-1);
  cursor: pointer;
  color: var(--color-gray-500);
  display: flex;
  align-items: center;
}

.expand-btn svg {
  width: 1rem;
  height: 1rem;
  transition: transform var(--transition-fast);
}

.expand-btn svg.rotated {
  transform: rotate(180deg);
}

.you-badge {
  font-size: var(--font-size-xs);
  background: var(--color-primary-100);
  color: var(--color-primary-700);
  padding: var(--spacing-1) var(--spacing-2);
  border-radius: var(--radius-sm);
  margin-left: var(--spacing-2);
}

.status-badge {
  display: inline-block;
  padding: var(--spacing-1) var(--spacing-2);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
}

.status-active {
  background: var(--color-green-100);
  color: var(--color-green-700);
}

.status-inactive {
  background: var(--color-gray-100);
  color: var(--color-gray-600);
}

.cell-date {
  color: var(--color-gray-500);
}

.cell-actions {
  text-align: right;
}

.actions-menu {
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacing-2);
  justify-content: flex-end;
}

.role-select {
  padding: var(--spacing-1) var(--spacing-2);
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  background: var(--color-white);
}

.icon-btn {
  background: none;
  border: none;
  padding: var(--spacing-1);
  cursor: pointer;
  color: var(--color-gray-500);
  display: inline-flex;
  align-items: center;
}

.icon-btn:hover {
  color: var(--color-text-primary);
}

.icon-btn.success {
  color: var(--color-green-600);
}

.icon-btn.success:hover {
  color: var(--color-green-700);
}

.icon-btn svg {
  width: 1rem;
  height: 1rem;
}
</style>
e>
