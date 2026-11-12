<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import AppHeader from './components/layout/AppHeader.vue'
import CenterManagementView from './views/centers/CenterManagementView.vue'
import MyCasesView from './views/cases/MyCasesView.vue'
import UserManagementView from './views/users/UserManagementView.vue'
import type { LoginResponse, UserRoleResponse } from './types/auth'

const accessToken = ref(sessionStorage.getItem('shaml_access_token'))
const activeUserRoleId = ref<number | null>(
  Number(sessionStorage.getItem('shaml_active_role_id')) || null,
)
const roles = ref<UserRoleResponse[]>([])
const username = ref('')
const password = ref('')
const loading = ref(false)
const errorMessage = ref('')
const permissions = ref<string[]>([])
const activeSection = ref<'home' | 'cases' | 'centers' | 'users'>('home')
const roleContextVersion = ref(0)
const isAuthenticated = computed(() => Boolean(accessToken.value))

async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  headers.set('Content-Type', 'application/json')
  if (accessToken.value) headers.set('Authorization', `Bearer ${accessToken.value}`)

  const response = await fetch(`/api/v1${path}`, { ...options, headers })
  if (!response.ok) throw new Error(`Request failed with status ${response.status}.`)
  return response.json() as Promise<T>
}

function saveSession(response: LoginResponse) {
  accessToken.value = response.accessToken
  activeUserRoleId.value = response.activeUserRoleId
  sessionStorage.setItem('shaml_access_token', response.accessToken)
  sessionStorage.setItem('shaml_active_role_id', String(response.activeUserRoleId))
}

function clearSession() {
  accessToken.value = null
  activeUserRoleId.value = null
  roles.value = []
  permissions.value = []
  activeSection.value = 'home'
  sessionStorage.removeItem('shaml_access_token')
  sessionStorage.removeItem('shaml_active_role_id')
}

async function loadRoles() {
  roles.value = await apiRequest<UserRoleResponse[]>('/auth/roles')
}

async function loadPermissions() {
  permissions.value = await apiRequest<string[]>('/auth/permissions')
}

function resetRoleContext() {
  const activeRole = roles.value.find(
    (role) => role.userRoleId === activeUserRoleId.value,
  )

  if (activeRole?.roleCode === 'PUBLIC_USER') {
    activeSection.value = 'cases'
  } else if (permissions.value.includes('CENTER_VIEW')) {
    activeSection.value = 'home'
  } else if (permissions.value.includes('CASE_VIEW')) {
    activeSection.value = 'cases'
  } else {
    activeSection.value = 'home'
  }

  roleContextVersion.value += 1
}

async function loadRoleContext() {
  await Promise.all([loadRoles(), loadPermissions()])
  resetRoleContext()
}

async function login() {
  loading.value = true
  errorMessage.value = ''
  try {
    const response = await apiRequest<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username: username.value, password: password.value }),
    })
    saveSession(response)
    password.value = ''
    await loadRoleContext()
  } catch (error) {
    clearSession()
    errorMessage.value = error instanceof Error ? error.message : 'Login failed.'
  } finally {
    loading.value = false
  }
}


async function switchRole(userRoleId: number) {
  if (userRoleId === activeUserRoleId.value || loading.value) return

  loading.value = true
  errorMessage.value = ''

  try {
    const response = await apiRequest<LoginResponse>(
        '/auth/switch-role',
        {
          method: 'POST',
          body: JSON.stringify({ userRoleId }),
        },
    )

    // The backend has invalidated the previous token.
    // Store the replacement before making any further requests.
    saveSession(response)

    // Remove permissions from the previous role immediately.
    roles.value = []
    permissions.value = []
    activeSection.value = 'home'
    roleContextVersion.value += 1

    try {
      await loadRoleContext()
    } catch {
      // The new JWT is valid, but its role context
      // could not be loaded. Do not restore the old JWT.
      clearSession()
      errorMessage.value =
          'Your role was changed, but the session could not be refreshed. Please sign in again.'
    }
  } catch (error) {
    errorMessage.value =
        error instanceof Error
            ? error.message
            : 'Unable to switch role.'
  } finally {
    loading.value = false
  }
}


onMounted(async () => {
  if (!accessToken.value) return
  try {
    await loadRoleContext()
  } catch {
    clearSession()
  }
})
</script>

<template>
  <div class="app-shell">
    <main v-if="!isAuthenticated" class="login-page">
      <section class="login-intro">
        <div>
          <span>SHAML PLATFORM</span>
          <h1>Care begins with connection.</h1>
          <p>A unified workspace for care centers, users, and services.</p>
        </div>
      </section>

      <form class="login-card" @submit.prevent="login">
        <div class="login-card__mark">S</div>
        <span class="eyebrow">WELCOME BACK</span>
        <h2>Sign in to Shaml</h2>
        <p class="hint">Enter your account details to continue.</p>

        <label>
          Username
          <input v-model="username" autocomplete="username" placeholder="Enter your username" required />
        </label>

        <label>
          Password
          <input v-model="password" autocomplete="current-password" placeholder="Enter your password" required type="password" />
        </label>

        <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>
        <button :disabled="loading" type="submit">
          {{ loading ? 'Signing in…' : 'Sign in' }}
        </button>
      </form>
    </main>

    <template v-else>
      <AppHeader
        :roles="roles"
        :active-user-role-id="activeUserRoleId"
        :active-section="activeSection"
        :permissions="permissions"
        :switching="loading"
        @switch-role="switchRole"
        @sign-out="clearSession"
        @navigate="activeSection = $event"
      />

      <template v-if="activeSection === 'home'">
      <section class="page-heading">
        <div>
          <nav>Home <span>›</span> Overview</nav>
          <h1>Home</h1>
          <p>Welcome to the Shaml management platform.</p>
        </div>
      </section>

      <main class="app-content">
        <section class="home-card">
          <header>
            <div>
              <span class="eyebrow">CURRENT ACCESS</span>
              <h2>Your role assignments</h2>
            </div>
            <span class="status">● API connected</span>
          </header>

          <div class="home-card__body">
            <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>

            <article
              v-for="role in roles"
              :key="role.userRoleId"
              class="role-row"
              :class="{ active: role.userRoleId === activeUserRoleId }"
            >
              <span class="role-icon">✓</span>
              <div>
                <h3>{{ role.roleName }}</h3>
                <p>{{ role.centerDisplayName ?? 'No center assigned' }}</p>
              </div>
              <span v-if="role.userRoleId === activeUserRoleId" class="badge">Active</span>
              <button v-else :disabled="loading" type="button" @click="switchRole(role.userRoleId)">
                Use role
              </button>
            </article>
          </div>
        </section>
      </main>
      </template>

      <MyCasesView
        v-else-if="activeSection === 'cases'"
        :key="`cases-${roleContextVersion}`"
      />

      <CenterManagementView
          v-else-if="activeSection === 'centers' && accessToken && permissions.includes('CENTER_VIEW')"
          :key="`centers-${roleContextVersion}`"
          :access-token="accessToken"
          :can-create="permissions.includes('CENTER_CREATE')"
          :can-update="permissions.includes('CENTER_UPDATE')"
      />

      <UserManagementView
        v-else-if="activeSection === 'users' && accessToken && permissions.includes('USER_VIEW')"
        :key="`users-${roleContextVersion}`"
        :access-token="accessToken"
      />
    </template>
  </div>
</template>

<style scoped>
.app-shell { min-height: 100vh; }
.login-page { display: grid; grid-template-columns: 1.05fr 0.95fr; min-height: 100vh; }
.login-intro { align-items: flex-end; background: linear-gradient(135deg, #116b4a, #19875f); color: white; display: flex; padding: 6rem; }
.login-intro div { max-width: 36rem; }
.login-intro span, .eyebrow { font-size: 0.72rem; font-weight: 750; letter-spacing: 0.14em; }
.login-intro h1 { font-size: 4rem; letter-spacing: -0.045em; line-height: 1; margin: 1rem 0 1.25rem; max-width: 10ch; }
.login-intro p { color: rgb(255 255 255 / 75%); font-size: 1.05rem; }
.login-card { align-content: center; background: var(--surface); display: grid; gap: 1rem; padding: 6rem; }
.login-card__mark { align-items: center; background: var(--brand); border-radius: 0.35rem; color: white; display: flex; font-size: 1.5rem; font-weight: 800; height: 3.4rem; justify-content: center; margin-bottom: 1rem; width: 3.4rem; }
.eyebrow { color: var(--brand); }
.login-card h2 { color: var(--ink); font-size: 2.4rem; letter-spacing: -0.035em; margin: 0; }
.hint { color: var(--muted); margin: -0.4rem 0 1rem; }
.login-card label { color: var(--ink); display: grid; font-size: 0.84rem; font-weight: 650; gap: 0.45rem; }
.login-card input { background: white; border: 1px solid var(--border-strong); border-radius: 0.3rem; color: var(--ink); min-height: 3.2rem; padding: 0.7rem 0.85rem; }
.login-card button { background: var(--brand); border: 0; border-radius: 0.3rem; color: white; cursor: pointer; font-weight: 700; margin-top: 0.5rem; min-height: 3.2rem; }
.page-heading { background: var(--mint); border-bottom: 1px solid var(--border); }
.page-heading > div { margin: 0 auto; max-width: 88rem; padding: 2rem 4rem 2.3rem; }
.page-heading nav { color: var(--muted); font-size: 0.78rem; margin-bottom: 1.5rem; }
.page-heading nav span { margin: 0 0.5rem; }
.page-heading h1 { color: var(--ink); font-size: 2.7rem; letter-spacing: -0.04em; margin: 0; }
.page-heading p { color: var(--muted); margin: 0.4rem 0 0; }
.app-content { margin: 0 auto; max-width: 88rem; padding: 2.3rem 4rem; }
.home-card { background: white; border: 1px solid var(--border); border-radius: 0.5rem; overflow: hidden; }
.home-card > header { align-items: center; background: var(--mint); border-bottom: 1px solid var(--border); display: flex; justify-content: space-between; padding: 1.35rem 1.6rem; }
.home-card h2 { color: var(--ink); font-size: 1.15rem; margin: 0.25rem 0 0; }
.status { color: var(--brand); font-size: 0.78rem; font-weight: 650; }
.home-card__body { display: grid; gap: 0.75rem; padding: 1.6rem; }
.role-row { align-items: center; border: 1px solid var(--border); border-radius: 0.35rem; display: grid; gap: 1rem; grid-template-columns: auto 1fr auto; padding: 1rem; }
.role-row.active { background: var(--mint); border-color: #79bca4; box-shadow: inset 4px 0 0 var(--brand); }
.role-icon { align-items: center; background: #e0f2eb; border-radius: 50%; color: var(--brand); display: flex; height: 2.5rem; justify-content: center; width: 2.5rem; }
.role-row h3 { color: var(--ink); font-size: 1rem; margin: 0; }
.role-row p { color: var(--muted); margin: 0.25rem 0 0; }
.badge { background: #dff3eb; border-radius: 999px; color: var(--brand); font-size: 0.7rem; font-weight: 750; padding: 0.3rem 0.65rem; }
.role-row button { background: white; border: 1px solid var(--brand); border-radius: 0.3rem; color: var(--brand); cursor: pointer; font-weight: 650; padding: 0.55rem 0.8rem; }
.error-message { background: #fff3f2; border: 1px solid #f2c3bf; border-radius: 0.3rem; color: #b53d34; padding: 0.7rem; }
button:disabled { cursor: wait; opacity: 0.55; }
</style>
