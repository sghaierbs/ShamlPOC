<script setup lang="ts">
import type { UserRoleResponse } from '../../types/auth'

defineProps<{
  roles: UserRoleResponse[]
  activeUserRoleId: number | null
  activeSection: 'home' | 'cases' | 'centers' | 'users'
  permissions: string[]
  switching?: boolean
}>()

const emit = defineEmits<{
  switchRole: [userRoleId: number]
  signOut: []
  navigate: [section: 'home' | 'cases' | 'centers' | 'users']
}>()

function handleRoleChange(event: Event) {
  const userRoleId = Number((event.target as HTMLSelectElement).value)
  if (Number.isSafeInteger(userRoleId) && userRoleId > 0) {
    emit('switchRole', userRoleId)
  }
}
</script>

<template>
  <header class="app-header">
    <a class="brand" href="/">
      <span class="brand__mark">S</span>
      <span class="brand__copy">
        <strong>SHAML</strong>
        <small>Care and Support Platform</small>
      </span>
    </a>

    <nav class="primary-nav" aria-label="Primary navigation">
      <button
        v-if="roles.find((role) => role.userRoleId === activeUserRoleId)?.roleCode !== 'PUBLIC_USER'"
        :class="{ active: activeSection === 'home' }"
        type="button"
        @click="emit('navigate', 'home')"
      >Home</button>
      <button
        v-if="permissions.includes('CASE_VIEW') && roles.find((role) => role.userRoleId === activeUserRoleId)?.roleCode !== 'ORGANISATION_DIRECTOR'"
        :class="{ active: activeSection === 'cases' }"
        type="button"
        @click="emit('navigate', 'cases')"
      >My Cases</button>
      <button
        v-if="permissions.includes('CENTER_VIEW')"
        :class="{ active: activeSection === 'centers' }"
        type="button"
        @click="emit('navigate', 'centers')"
      >Center Management</button>
      <button
        v-if="permissions.includes('USER_VIEW')"
        :class="{ active: activeSection === 'users' }"
        type="button"
        @click="emit('navigate', 'users')"
      >User Management</button>
    </nav>

    <div class="header-actions">
      <label class="role-selector">
        <span class="sr-only">Active role</span>
        <select
          :value="activeUserRoleId ?? ''"
          :disabled="switching"
          @change="handleRoleChange"
        >
          <option disabled value="">Select role</option>
          <option
            v-for="role in roles.filter((item) => item.isActive)"
            :key="role.userRoleId"
            :value="role.userRoleId"
          >
            {{ role.roleName }}
            {{ role.centerDisplayName ? `— ${role.centerDisplayName}` : '' }}
          </option>
        </select>
      </label>

      <button class="sign-out" type="button" @click="emit('signOut')">
        Sign out
      </button>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  align-items: center;
  background: var(--surface);
  border-bottom: 1px solid var(--border);
  display: flex;
  min-height: 5rem;
  padding: 0 4rem;
}

.brand {
  align-items: center;
  color: var(--ink);
  display: flex;
  gap: 0.7rem;
  min-width: 15rem;
  text-decoration: none;
}

.brand__mark {
  align-items: center;
  background: var(--brand);
  border-radius: 0.3rem;
  color: white;
  display: flex;
  font-size: 1.3rem;
  font-weight: 800;
  height: 3rem;
  justify-content: center;
  width: 3rem;
}

.brand__copy {
  display: grid;
}

.brand strong {
  letter-spacing: 0.12em;
}

.brand small {
  color: var(--muted);
  font-size: 0.66rem;
}

.primary-nav {
  align-self: stretch;
  display: flex;
  margin: 0 auto;
}

.primary-nav button {
  align-items: center;
  background: transparent;
  border: 0;
  color: var(--ink);
  cursor: pointer;
  display: flex;
  font-size: 0.9rem;
  font-weight: 650;
  padding: 0 1.35rem;
}

.primary-nav button.active {
  background: var(--brand);
  color: white;
}

.primary-nav button:disabled { cursor: default; opacity: .45; }

.header-actions {
  align-items: center;
  display: flex;
  gap: 0.6rem;
  justify-content: flex-end;
  min-width: 21rem;
}

.role-selector select {
  background: var(--surface);
  border: 1px solid var(--border-strong);
  border-radius: 0.3rem;
  color: var(--ink);
  min-height: 2.65rem;
  min-width: 14rem;
  padding: 0.45rem 0.65rem;
}

.sign-out {
  background: var(--surface);
  border: 1px solid #efc1bd;
  border-radius: 0.3rem;
  color: #c9473d;
  cursor: pointer;
  min-height: 2.65rem;
  padding: 0 0.8rem;
}

.sr-only {
  clip: rect(0, 0, 0, 0);
  height: 1px;
  overflow: hidden;
  position: absolute;
  white-space: nowrap;
  width: 1px;
}
</style>
