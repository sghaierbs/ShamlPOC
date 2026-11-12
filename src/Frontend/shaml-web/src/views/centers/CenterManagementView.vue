<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import type {
  CenterDto,
  CenterFormModel,
  GeographicLookup,
} from '../../types/centers'

const props = defineProps<{ accessToken: string }>()

type Mode = 'list' | 'view' | 'create' | 'edit'

const mode = ref<Mode>('list')
const centers = ref<CenterDto[]>([])
const selectedCenter = ref<CenterDto | null>(null)
const regions = ref<GeographicLookup[]>([])
const cities = ref<GeographicLookup[]>([])
const districts = ref<GeographicLookup[]>([])
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const form = reactive<CenterFormModel>({
  name: '',
  regionId: null,
  cityId: null,
  districtId: null,
  operationalCapacity: 1,
  waitingListLimit: 0,
  directorSocialId: '',
  isActive: true,
  isReceivingCases: true,
})

const title = computed(() => {
  if (mode.value === 'create') return 'Create Center'
  if (mode.value === 'edit') return 'Update Center'
  if (mode.value === 'view') return 'Center Details'
  return 'Center Management'
})

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
      // Keep the status-based message when the response has no JSON body.
    }
    throw new Error(message)
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

async function loadCenters() {
  loading.value = true
  errorMessage.value = ''
  try {
    centers.value = await api<CenterDto[]>('/centers')
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Unable to load centers.'
  } finally {
    loading.value = false
  }
}

async function loadRegions() {
  if (regions.value.length) return
  regions.value = await api<GeographicLookup[]>('/lookups/regions')
}

async function loadCities(regionId: number) {
  cities.value = await api<GeographicLookup[]>(`/lookups/cities?regionId=${regionId}`)
}

async function loadDistricts(cityId: number) {
  districts.value = await api<GeographicLookup[]>(`/lookups/districts?cityId=${cityId}`)
}

async function onRegionChange() {
  form.cityId = null
  form.districtId = null
  cities.value = []
  districts.value = []
  if (form.regionId) await loadCities(form.regionId)
}

async function onCityChange() {
  form.districtId = null
  districts.value = []
  if (form.cityId) await loadDistricts(form.cityId)
}

function resetForm() {
  Object.assign(form, {
    name: '',
    regionId: null,
    cityId: null,
    districtId: null,
    operationalCapacity: 1,
    waitingListLimit: 0,
    directorSocialId: '',
    isActive: true,
    isReceivingCases: true,
  })
  cities.value = []
  districts.value = []
}

async function openCreate() {
  errorMessage.value = ''
  successMessage.value = ''
  resetForm()
  await loadRegions()
  mode.value = 'create'
}

async function openView(center: CenterDto) {
  errorMessage.value = ''
  selectedCenter.value = await api<CenterDto>(`/centers/${center.id}`)
  mode.value = 'view'
}

async function openEdit(center: CenterDto) {
  errorMessage.value = ''
  selectedCenter.value = await api<CenterDto>(`/centers/${center.id}`)
  const current = selectedCenter.value
  await loadRegions()
  await loadCities(current.regionId)
  if (current.districtId) await loadDistricts(current.cityId)

  Object.assign(form, {
    name: current.name,
    regionId: current.regionId,
    cityId: current.cityId,
    districtId: current.districtId,
    operationalCapacity: current.operationalCapacity,
    waitingListLimit: current.waitingListLimit,
    directorSocialId: current.directorSocialId ?? '',
    isActive: current.isActive,
    isReceivingCases: current.isReceivingCases,
  })
  mode.value = 'edit'
}

function backToList() {
  mode.value = 'list'
  selectedCenter.value = null
  errorMessage.value = ''
}

function centerPayload() {
  return {
    name: form.name.trim(),
    regionId: form.regionId,
    cityId: form.cityId,
    districtId: form.districtId,
    operationalCapacity: Number(form.operationalCapacity),
    waitingListLimit: Number(form.waitingListLimit),
  }
}

async function submitForm() {
  if (!form.regionId || !form.cityId) {
    errorMessage.value = 'Region and city are required.'
    return
  }

  if (form.directorSocialId && !/^\d{10}$/.test(form.directorSocialId)) {
    errorMessage.value = 'Director Social ID must contain exactly 10 digits.'
    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    if (mode.value === 'create') {
      await api<CenterDto>('/centers', {
        method: 'POST',
        body: JSON.stringify({
          ...centerPayload(),
          directorSocialId: form.directorSocialId || null,
        }),
      })
      successMessage.value = 'Center created successfully.'
    } else if (selectedCenter.value) {
      await api<CenterDto>(`/centers/${selectedCenter.value.id}`, {
        method: 'PUT',
        body: JSON.stringify({
          ...centerPayload(),
          isActive: form.isActive,
          isReceivingCases: form.isActive && form.isReceivingCases,
        }),
      })

      const previousDirector = selectedCenter.value.directorSocialId ?? ''
      if (previousDirector !== form.directorSocialId) {
        await api<CenterDto>(`/centers/${selectedCenter.value.id}/director`, {
          method: 'PUT',
          body: JSON.stringify({
            directorSocialId: form.directorSocialId || null,
          }),
        })
      }
      successMessage.value = 'Center updated successfully.'
    }

    await loadCenters()
    mode.value = 'list'
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Unable to save the center.'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadCenters(), loadRegions()])
})
</script>

<template>
  <section class="page-heading">
    <div>
      <nav>Home <span>›</span> Center Management</nav>
      <h1>{{ title }}</h1>
      <p>Create, review, and update Shaml service centers.</p>
    </div>
  </section>

  <main class="content">
    <p v-if="successMessage" class="message success">{{ successMessage }}</p>
    <p v-if="errorMessage" class="message error">{{ errorMessage }}</p>

    <section v-if="mode === 'list'" class="card">
      <header class="card-header">
        <div>
          <span class="eyebrow">CENTERS</span>
          <h2>Registered centers</h2>
        </div>
        <button class="primary" type="button" @click="openCreate">Create center</button>
      </header>

      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Center</th>
              <th>Region / City</th>
              <th>Capacity</th>
              <th>Waiting limit</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!loading && centers.length === 0">
              <td colspan="6" class="empty">No centers have been created.</td>
            </tr>
            <tr v-for="center in centers" :key="center.id">
              <td><strong>{{ center.name }}</strong><small>#{{ center.id }}</small></td>
              <td>Region {{ center.regionId }} / City {{ center.cityId }}</td>
              <td>{{ center.operationalCapacity }}</td>
              <td>{{ center.waitingListLimit }}</td>
              <td><span class="status" :class="{ inactive: !center.isActive }">{{ center.isActive ? 'Active' : 'Inactive' }}</span></td>
              <td class="actions">
                <button type="button" @click="openView(center)">View</button>
                <button type="button" @click="openEdit(center)">Update</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section v-else-if="mode === 'view' && selectedCenter" class="card">
      <header class="card-header">
        <div><span class="eyebrow">CENTER DETAILS</span><h2>{{ selectedCenter.name }}</h2></div>
        <div class="header-actions"><button type="button" @click="backToList">Back</button><button class="primary" type="button" @click="openEdit(selectedCenter)">Update center</button></div>
      </header>
      <dl class="details-grid">
        <div><dt>Center ID</dt><dd>{{ selectedCenter.id }}</dd></div>
        <div><dt>Display name</dt><dd>{{ selectedCenter.displayName }}</dd></div>
        <div><dt>Region ID</dt><dd>{{ selectedCenter.regionId }}</dd></div>
        <div><dt>City ID</dt><dd>{{ selectedCenter.cityId }}</dd></div>
        <div><dt>District ID</dt><dd>{{ selectedCenter.districtId ?? 'Not assigned' }}</dd></div>
        <div><dt>Director Social ID</dt><dd>{{ selectedCenter.directorSocialId ?? 'Not assigned' }}</dd></div>
        <div><dt>Operational capacity</dt><dd>{{ selectedCenter.operationalCapacity }}</dd></div>
        <div><dt>Waiting list limit</dt><dd>{{ selectedCenter.waitingListLimit }}</dd></div>
        <div><dt>Center status</dt><dd>{{ selectedCenter.isActive ? 'Active' : 'Inactive' }}</dd></div>
        <div><dt>Receiving cases</dt><dd>{{ selectedCenter.isReceivingCases ? 'Enabled' : 'Disabled' }}</dd></div>
      </dl>
    </section>

    <form v-else class="card" @submit.prevent="submitForm">
      <header class="card-header">
        <div><span class="eyebrow">CENTER INFORMATION</span><h2>{{ mode === 'create' ? 'New center' : 'Update center' }}</h2></div>
      </header>

      <div class="form-grid">
        <label class="wide">Center name <span>*</span><input v-model="form.name" maxlength="200" required /></label>
        <label>Region <span>*</span><select v-model="form.regionId" required @change="onRegionChange"><option :value="null" disabled>Select region</option><option v-for="item in regions" :key="item.id" :value="item.id">{{ item.nameEn }}</option></select></label>
        <label>City <span>*</span><select v-model="form.cityId" :disabled="!form.regionId" required @change="onCityChange"><option :value="null" disabled>Select city</option><option v-for="item in cities" :key="item.id" :value="item.id">{{ item.nameEn }}</option></select></label>
        <label>District<select v-model="form.districtId" :disabled="!form.cityId"><option :value="null">No district</option><option v-for="item in districts" :key="item.id" :value="item.id">{{ item.nameEn }}</option></select></label>
        <label>Director Social ID<input v-model="form.directorSocialId" inputmode="numeric" maxlength="10" placeholder="10 digits" /></label>
        <label>Operational capacity <span>*</span><input v-model.number="form.operationalCapacity" min="1" required type="number" /></label>
        <label>Waiting list limit <span>*</span><input v-model.number="form.waitingListLimit" min="0" required type="number" /></label>
        <template v-if="mode === 'edit'">
          <label>Center status<select v-model="form.isActive"><option :value="true">Active</option><option :value="false">Inactive</option></select></label>
          <label>Case reception<select v-model="form.isReceivingCases" :disabled="!form.isActive"><option :value="true">Enabled</option><option :value="false">Disabled</option></select></label>
        </template>
      </div>

      <footer class="form-actions"><button type="button" @click="backToList">Cancel</button><button class="primary" :disabled="loading" type="submit">{{ loading ? 'Saving…' : 'Save center' }}</button></footer>
    </form>
  </main>
</template>

<style scoped>
.page-heading { background: var(--mint); border-bottom: 1px solid var(--border); }
.page-heading > div, .content { margin: 0 auto; max-width: 88rem; padding: 2rem 4rem; }
.page-heading nav { color: var(--muted); font-size: .78rem; margin-bottom: 1.4rem; }
.page-heading nav span { margin: 0 .5rem; }
.page-heading h1 { color: var(--ink); font-size: 2.5rem; margin: 0; }
.page-heading p { color: var(--muted); margin: .35rem 0 0; }
.card { background: white; border: 1px solid var(--border); border-radius: .5rem; overflow: hidden; }
.card-header { align-items: center; background: var(--mint); border-bottom: 1px solid var(--border); display: flex; justify-content: space-between; padding: 1.3rem 1.6rem; }
.eyebrow { color: var(--brand); font-size: .7rem; font-weight: 750; letter-spacing: .13em; }
.card-header h2 { color: var(--ink); font-size: 1.15rem; margin: .2rem 0 0; }
button { background: white; border: 1px solid var(--border-strong); border-radius: .3rem; color: var(--ink); cursor: pointer; padding: .55rem .85rem; }
button.primary { background: var(--brand); border-color: var(--brand); color: white; font-weight: 700; }
.table-wrap { padding: 1rem 1.6rem 1.6rem; }
table { border-collapse: collapse; width: 100%; }
th, td { border-bottom: 1px solid var(--border); padding: .9rem .7rem; text-align: left; }
th { color: var(--muted); font-size: .75rem; text-transform: uppercase; }
td { color: var(--text); font-size: .88rem; }
td strong, td small { display: block; }
td small { color: var(--muted); margin-top: .2rem; }
.actions { display: flex; gap: .4rem; }
.status { background: #dff3eb; border-radius: 999px; color: var(--brand); font-size: .7rem; font-weight: 700; padding: .25rem .55rem; }
.status.inactive { background: #eef0f1; color: #687680; }
.empty { color: var(--muted); padding: 2rem; text-align: center; }
.details-grid { display: grid; grid-template-columns: repeat(2, 1fr); margin: 0; padding: 1.6rem; }
.details-grid div { border-bottom: 1px solid var(--border); padding: 1rem .5rem; }
dt { color: var(--muted); font-size: .75rem; margin-bottom: .3rem; }
dd { color: var(--ink); font-weight: 600; margin: 0; }
.header-actions, .form-actions { display: flex; gap: .55rem; }
.form-grid { display: grid; gap: 1.2rem; grid-template-columns: repeat(2, 1fr); padding: 1.7rem; }
.form-grid label { color: var(--ink); display: grid; font-size: .82rem; font-weight: 650; gap: .4rem; }
.form-grid label span { color: #c9473d; }
.form-grid .wide { grid-column: 1 / -1; }
input, select { background: white; border: 1px solid var(--border-strong); border-radius: .3rem; color: var(--ink); min-height: 3rem; padding: .65rem .75rem; }
input:disabled, select:disabled { background: #f4f6f6; }
.form-actions { border-top: 1px solid var(--border); justify-content: flex-end; padding: 1.2rem 1.7rem; }
.message { border-radius: .3rem; margin: 0 0 1rem; padding: .75rem 1rem; }
.message.error { background: #fff3f2; border: 1px solid #f2c3bf; color: #b53d34; }
.message.success { background: #eaf7f2; border: 1px solid #b9dfd1; color: #166b4d; }
</style>
