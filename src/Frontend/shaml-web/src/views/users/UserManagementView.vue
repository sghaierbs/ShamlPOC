<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { CenterDto } from '../../types/centers'
import type { AvailableRole, ManagedUser } from '../../types/users'

const props = defineProps<{ accessToken: string }>()

const users = ref<ManagedUser[]>([])
const selectedUser = ref<ManagedUser | null>(null)
const availableRoles = ref<AvailableRole[]>([])
const centers = ref<CenterDto[]>([])
const selectedRoleId = ref<number | null>(null)
const selectedCenterId = ref<number | null>(null)
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const selectedRole = computed(() =>
  availableRoles.value.find((role) => role.roleId === selectedRoleId.value),
)

const requiresCenter = computed(() => selectedRole.value?.roleScope === 2)

async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  headers.set('Content-Type', 'application/json')
  headers.set('Authorization', `Bearer ${props.accessToken}`)
  const response = await fetch(`/api/v1${path}`, { ...options, headers })

  if (!response.ok) {
    let message = `Request failed with status ${response.status}.`
    try {
      const body = await response.json() as { message?: string; title?: string }
      message = body.message ?? body.title ?? message
    } catch {
      // Keep the status message when no JSON error body exists.
    }
    throw new Error(message)
  }

  return response.json() as Promise<T>
}

async function loadUsers() {
  loading.value = true
  errorMessage.value = ''
  try {
    users.value = await api<ManagedUser[]>('/users')
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Unable to load users.'
  } finally {
    loading.value = false
  }
}

async function openUser(user: ManagedUser) {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    const [details, roles, centerList] = await Promise.all([
      api<ManagedUser>(`/users/${user.socialId}`),
      api<AvailableRole[]>('/users/available-roles'),
      api<CenterDto[]>('/centers'),
    ])
    selectedUser.value = details
    availableRoles.value = roles
    centers.value = centerList.filter((center) => center.isActive)
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Unable to open the user.'
  } finally {
    loading.value = false
  }
}

function closeUser() {
  selectedUser.value = null
  selectedRoleId.value = null
  selectedCenterId.value = null
  errorMessage.value = ''
}

function onRoleChange() {
  selectedCenterId.value = null
}

async function addRole() {
  if (!selectedUser.value || !selectedRoleId.value) return
  if (requiresCenter.value && !selectedCenterId.value) {
    errorMessage.value = 'Select a center for this role.'
    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    selectedUser.value = await api<ManagedUser>(
      `/users/${selectedUser.value.socialId}/roles`,
      {
        method: 'POST',
        body: JSON.stringify({
          roleId: selectedRoleId.value,
          centerId: requiresCenter.value ? selectedCenterId.value : null,
        }),
      },
    )
    selectedRoleId.value = null
    selectedCenterId.value = null
    successMessage.value = 'Role assigned successfully.'
    await loadUsers()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Unable to assign the role.'
  } finally {
    loading.value = false
  }
}

async function withdrawRole(userRoleId: number) {
  if (!selectedUser.value) return
  if (!window.confirm('Withdraw this role assignment from the user?')) return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    selectedUser.value = await api<ManagedUser>(
      `/users/${selectedUser.value.socialId}/roles/${userRoleId}`,
      { method: 'DELETE' },
    )
    successMessage.value = 'Role withdrawn successfully.'
    await loadUsers()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Unable to withdraw the role.'
  } finally {
    loading.value = false
  }
}

onMounted(loadUsers)
</script>

<template>
  <section class="page-heading">
    <div>
      <nav>Home <span>›</span> User Management</nav>
      <h1>{{ selectedUser ? 'User Details' : 'User Management' }}</h1>
      <p>Review users and manage their active role assignments.</p>
    </div>
  </section>

  <main class="content">
    <p v-if="successMessage" class="message success">{{ successMessage }}</p>
    <p v-if="errorMessage" class="message error">{{ errorMessage }}</p>

    <section v-if="!selectedUser" class="card">
      <header class="card-header">
        <div><span class="eyebrow">USERS</span><h2>Available users</h2></div>
        <span class="count">{{ users.length }} users</span>
      </header>

      <div class="table-wrap">
        <table>
          <thead><tr><th>User</th><th>Social ID</th><th>Status</th><th>Roles</th><th>Action</th></tr></thead>
          <tbody>
            <tr v-if="!loading && users.length === 0"><td class="empty" colspan="5">No users found.</td></tr>
            <tr v-for="user in users" :key="user.socialId">
              <td><strong>{{ user.fullName }}</strong><small>{{ user.username }}</small></td>
              <td>{{ user.socialId }}</td>
              <td><span class="status" :class="{ inactive: !user.isActive }">{{ user.isActive ? 'Active' : 'Inactive' }}</span></td>
              <td>{{ user.roles.length }}</td>
              <td><button type="button" @click="openUser(user)">Open</button></td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <template v-else>
      <section class="card user-summary">
        <header class="card-header">
          <div><span class="eyebrow">USER PROFILE</span><h2>{{ selectedUser.fullName }}</h2></div>
          <button type="button" @click="closeUser">Back to users</button>
        </header>
        <dl>
          <div><dt>Username</dt><dd>{{ selectedUser.username }}</dd></div>
          <div><dt>Social ID</dt><dd>{{ selectedUser.socialId }}</dd></div>
          <div><dt>Status</dt><dd>{{ selectedUser.isActive ? 'Active' : 'Inactive' }}</dd></div>
          <div><dt>Active roles</dt><dd>{{ selectedUser.roles.length }}</dd></div>
        </dl>
      </section>

      <section class="card roles-card">
        <header class="card-header"><div><span class="eyebrow">ACCESS</span><h2>Role assignments</h2></div></header>
        <div class="roles-body">
          <article v-for="role in selectedUser.roles" :key="role.userRoleId" class="role-row">
            <div><h3>{{ role.roleName }}</h3><p>{{ role.centerDisplayName ?? 'No center assigned' }}</p></div>
            <span v-if="role.isDefault" class="default-badge">Default</span>
            <button class="danger" :disabled="loading" type="button" @click="withdrawRole(role.userRoleId)">Withdraw</button>
          </article>

          <form class="add-role" @submit.prevent="addRole">
            <label>New role<select v-model="selectedRoleId" required @change="onRoleChange"><option :value="null" disabled>Select role</option><option v-for="role in availableRoles" :key="role.roleId" :value="role.roleId">{{ role.roleName }}</option></select></label>
            <label v-if="requiresCenter">Center<select v-model="selectedCenterId" required><option :value="null" disabled>Select center</option><option v-for="center in centers" :key="center.id" :value="center.id">{{ center.displayName }}</option></select></label>
            <button class="primary" :disabled="loading || !selectedRoleId" type="submit">Add role</button>
          </form>
        </div>
      </section>
    </template>
  </main>
</template>

<style scoped>
.page-heading { background: var(--mint); border-bottom: 1px solid var(--border); }
.page-heading > div, .content { margin: 0 auto; max-width: 88rem; padding: 2rem 4rem; }
.page-heading nav { color: var(--muted); font-size: .78rem; margin-bottom: 1.4rem; }
.page-heading nav span { margin: 0 .5rem; }
.page-heading h1 { color: var(--ink); font-size: 2.5rem; margin: 0; }
.page-heading p { color: var(--muted); margin: .35rem 0 0; }
.card { background: white; border: 1px solid var(--border); border-radius: .5rem; margin-bottom: 1rem; overflow: hidden; }
.card-header { align-items: center; background: var(--mint); border-bottom: 1px solid var(--border); display: flex; justify-content: space-between; padding: 1.3rem 1.6rem; }
.eyebrow { color: var(--brand); font-size: .7rem; font-weight: 750; letter-spacing: .13em; }
.card-header h2 { color: var(--ink); font-size: 1.15rem; margin: .2rem 0 0; }
button { background: white; border: 1px solid var(--border-strong); border-radius: .3rem; color: var(--ink); cursor: pointer; padding: .55rem .85rem; }
button.primary { background: var(--brand); border-color: var(--brand); color: white; font-weight: 700; }
button.danger { border-color: #efc1bd; color: #c9473d; }
button:disabled { cursor: wait; opacity: .5; }
.count { color: var(--muted); font-size: .8rem; }
.table-wrap { padding: 1rem 1.6rem 1.6rem; }
table { border-collapse: collapse; width: 100%; }
th, td { border-bottom: 1px solid var(--border); padding: .9rem .7rem; text-align: left; }
th { color: var(--muted); font-size: .75rem; text-transform: uppercase; }
td { color: var(--text); font-size: .88rem; }
td strong, td small { display: block; }
td small { color: var(--muted); margin-top: .2rem; }
.status, .default-badge { background: #dff3eb; border-radius: 999px; color: var(--brand); font-size: .7rem; font-weight: 700; padding: .25rem .55rem; }
.status.inactive { background: #eef0f1; color: #687680; }
.empty { color: var(--muted); padding: 2rem; text-align: center; }
.user-summary dl { display: grid; grid-template-columns: repeat(4, 1fr); margin: 0; padding: 1.4rem 1.6rem; }
.user-summary dl div { padding-right: 1rem; }
dt { color: var(--muted); font-size: .75rem; margin-bottom: .3rem; }
dd { color: var(--ink); font-weight: 650; margin: 0; }
.roles-body { display: grid; gap: .75rem; padding: 1.5rem 1.6rem; }
.role-row { align-items: center; border: 1px solid var(--border); border-radius: .35rem; display: grid; gap: 1rem; grid-template-columns: 1fr auto auto; padding: 1rem; }
.role-row h3 { color: var(--ink); font-size: .95rem; margin: 0; }
.role-row p { color: var(--muted); margin: .25rem 0 0; }
.add-role { align-items: end; background: var(--mint); border: 1px solid var(--border); border-radius: .35rem; display: flex; gap: .8rem; margin-top: .5rem; padding: 1rem; }
.add-role label { color: var(--ink); display: grid; flex: 1; font-size: .8rem; font-weight: 650; gap: .4rem; }
select { background: white; border: 1px solid var(--border-strong); border-radius: .3rem; color: var(--ink); min-height: 2.8rem; padding: .55rem .7rem; }
.message { border-radius: .3rem; margin: 0 0 1rem; padding: .75rem 1rem; }
.message.error { background: #fff3f2; border: 1px solid #f2c3bf; color: #b53d34; }
.message.success { background: #eaf7f2; border: 1px solid #b9dfd1; color: #166b4d; }
</style>
