<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import UiInput from '~/components/ui/Input.vue'
import UiSelect from '~/components/ui/Select.vue'

const props = defineProps<{
  isOpen: boolean
  workspaceId: string
  roleOptions: { value: string; label: string }[]
}>()

const emit = defineEmits<{
  close: []
  invited: []
}>()

const email = ref('')
const role = ref('Viewer')
const isSubmitting = ref(false)
const formError = ref('')

const { inviteMember } = useMembers()

const isValidEmail = computed(() => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email.value)
})

const canSubmit = computed(() => isValidEmail.value && !isSubmitting.value)

async function handleSubmit() {
  if (!canSubmit.value) return

  isSubmitting.value = true
  formError.value = ''

  const result = await inviteMember(props.workspaceId, email.value, role.value)

  isSubmitting.value = false

  if (result.success) {
    email.value = ''
    role.value = 'Viewer'
    emit('invited')
    emit('close')
  } else {
    formError.value = result.error || 'Failed to send invite'
  }
}

function handleClose() {
  email.value = ''
  role.value = 'Viewer'
  formError.value = ''
  emit('close')
}
</script>

<template>
  <Teleport to="body">
    <div v-if="isOpen" class="modal-backdrop" @click.self="handleClose">
      <div class="modal" role="dialog" aria-modal="true" aria-labelledby="invite-modal-title">
        <div class="modal__header">
          <h2 id="invite-modal-title" class="modal__title">Invite Team Member</h2>
          <button class="modal__close" aria-label="Close" @click="handleClose">
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
        <form @submit.prevent="handleSubmit" class="modal__body">
          <UiInput
            id="invite-email"
            v-model="email"
            type="email"
            label="Email address"
            hint="colleague@company.com"
            required
            :error="email.length > 0 && !isValidEmail ? 'Please enter a valid email address' : ''"
          />
          <UiSelect
            id="invite-role"
            v-model="role"
            name="role"
            label="Role"
            required
            :options="roleOptions"
          />
          <p v-if="formError" class="modal__error" role="alert">{{ formError }}</p>
        </form>
        <div class="modal__footer">
          <UiButton variant="secondary" @click="handleClose">Cancel</UiButton>
          <UiButton type="submit" :disabled="!canSubmit" :loading="isSubmitting" @click="handleSubmit">
            Send Invite
          </UiButton>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: var(--z-modal, 100);
}

.modal {
  background: var(--color-white);
  border-radius: var(--radius-xl);
  width: 100%;
  max-width: 420px;
  box-shadow: var(--shadow-xl);
}

.modal__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-6);
  border-bottom: 1px solid var(--color-gray-200);
}

.modal__title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  margin: 0;
}

.modal__close {
  background: none;
  border: none;
  padding: var(--spacing-2);
  cursor: pointer;
  color: var(--color-gray-500);
  border-radius: var(--radius-md);
  transition: all var(--transition-fast);
}

.modal__close:hover {
  background: var(--color-gray-100);
  color: var(--color-gray-700);
}

.modal__close svg {
  width: 1.25rem;
  height: 1.25rem;
}

.modal__body {
  padding: var(--spacing-6);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
}

.modal__error {
  color: var(--color-error);
  font-size: var(--font-size-sm);
  margin: 0;
}

.modal__footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-3);
  padding: var(--spacing-6);
  border-top: 1px solid var(--color-gray-200);
}
</style>
