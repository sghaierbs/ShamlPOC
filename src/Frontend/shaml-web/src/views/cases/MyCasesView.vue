
<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import type {
  AssignedCase,
  PagedCasesResponse,
} from '../../types/cases'

const props = defineProps<{
  accessToken: string
}>()

const cases = ref<AssignedCase[]>([])
const loading = ref(false)
const error = ref('')
const search = ref('')
const status = ref('')
const page = ref(1)
const pageSize = 20
const totalCount = ref(0)

const totalPages = computed(() =>
    Math.max(1, Math.ceil(totalCount.value / pageSize)),
)

let requestController: AbortController | null = null

async function loadCases() {
  requestController?.abort()
  const controller = new AbortController()
  requestController = controller

  loading.value = true
  error.value = ''

  const params = new URLSearchParams({
    page: String(page.value),
    pageSize: String(pageSize),
  })

  if (search.value.trim()) {
    params.set('search', search.value.trim())
  }

  if (status.value) {
    params.set('status', status.value)
  }

  try {
    const response = await fetch(
        `/api/v1/cases/my?${params}`,
        {
          headers: {
            Authorization: `Bearer ${props.accessToken}`,
          },
          signal: controller.signal,
        },
    )

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`)
    }

    const data =
        (await response.json()) as PagedCasesResponse

    if (requestController !== controller) return

    cases.value = data.items
    totalCount.value = data.totalCount
  } catch (exception) {
    if (controller.signal.aborted) return

    cases.value = []
    totalCount.value = 0
    error.value = exception instanceof Error
        ? exception.message
        : 'Unable to load cases.'
  } finally {
    if (requestController === controller) {
      loading.value = false
      requestController = null
    }
  }
}

function applyFilters() {
  page.value = 1
  void loadCases()
}

function changePage(next: number) {
  if (next < 1 || next > totalPages.value) return
  page.value = next
  void loadCases()
}

function formatDate(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime())
      ? value
      : date.toLocaleDateString('en-GB')
}

onMounted(() => void loadCases())
onUnmounted(() => requestController?.abort())
</script>

<template>
  <section class="page-heading">
    <div>
      <nav>Home › My Cases</nav>
      <h1>My Cases</h1>
      <p>Cases assigned to your active center role.</p>
    </div>
  </section>

  <main class="content">
    <section class="card">
      <header class="card-header">
        <div>
          <span class="eyebrow">CASE MANAGEMENT</span>
          <h2>Assigned Cases</h2>
        </div>
        <button :disabled="loading" @click="loadCases">
          Refresh
        </button>
      </header>

      <form class="filters" @submit.prevent="applyFilters">
        <input
            v-model="search"
            placeholder="Search by case number"
        />

        <input
            v-model="status"
            placeholder="Case status"
        />

        <button type="submit">Search</button>
      </form>

      <div v-if="loading" class="message">
        Loading cases...
      </div>

      <div v-else-if="error" class="message error">
        Unable to load cases: {{ error }}
        <button @click="loadCases">Retry</button>
      </div>

      <div v-else-if="cases.length === 0" class="message">
        <h3>No assigned cases</h3>
        <p>No cases match your current filters.</p>
      </div>

      <template v-else>
        <div class="table-container">
          <table>
            <thead>
            <tr>
              <th>Case Number</th>
              <th>Status</th>
              <th>Assigned At</th>
              <th>Created At</th>
            </tr>
            </thead>
            <tbody>
            <tr v-for="item in cases" :key="item.id">
              <td class="case-number">
                {{ item.caseNumber }}
              </td>
              <td>
                  <span class="status">
                    {{ item.status }}
                  </span>
              </td>
              <td>{{ formatDate(item.assignedAtUtc) }}</td>
              <td>{{ formatDate(item.createdAtUtc) }}</td>
            </tr>
            </tbody>
          </table>
        </div>

        <footer class="pagination">
          <span>
            {{ totalCount }} cases ·
            Page {{ page }} of {{ totalPages }}
          </span>
          <div>
            <button
                :disabled="page === 1 || loading"
                @click="changePage(page - 1)"
            >
              Previous
            </button>
            <button
                :disabled="page >= totalPages || loading"
                @click="changePage(page + 1)"
            >
              Next
            </button>
          </div>
        </footer>
      </template>
    </section>
  </main>
</template>

<style scoped>
.page-heading {
  background: var(--mint);
  border-bottom: 1px solid var(--border);
}
.page-heading > div,
.content {
  max-width: 88rem;
  margin: 0 auto;
  padding: 2rem 4rem;
}
.page-heading h1 {
  font-size: 2.5rem;
  margin: 1rem 0 .3rem;
}
.page-heading p,
.page-heading nav {
  color: var(--muted);
}
.card {
  background: white;
  border: 1px solid var(--border);
  border-radius: .5rem;
  overflow: hidden;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: var(--mint);
  padding: 1.3rem 1.6rem;
}
.card-header h2 {
  margin: .25rem 0 0;
  font-size: 1.15rem;
}
.eyebrow {
  color: var(--brand);
  font-size: .7rem;
  font-weight: 750;
}
.filters {
  display: flex;
  gap: 1rem;
  padding: 1.5rem;
  flex-wrap: wrap;
}
input {
  flex: 1;
  min-width: 12rem;
  padding: .7rem;
  border: 1px solid var(--border-strong);
  border-radius: .3rem;
}
button {
  cursor: pointer;
  padding: .7rem 1rem;
  border: 1px solid var(--border-strong);
  border-radius: .3rem;
  background: white;
}
.filters button {
  background: var(--brand);
  color: white;
}
button:disabled {
  opacity: .5;
  cursor: not-allowed;
}
.table-container {
  overflow-x: auto;
}
table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
}
th, td {
  padding: 1rem 1.5rem;
  border-bottom: 1px solid var(--border);
}
th {
  background: #f8faf9;
  color: var(--muted);
  font-size: .8rem;
}
.case-number {
  color: var(--brand);
  font-weight: 700;
}
.status {
  background: var(--mint);
  border-radius: 999px;
  padding: .35rem .7rem;
  font-size: .8rem;
}
.message {
  padding: 4rem 2rem;
  text-align: center;
  color: var(--muted);
}
.error {
  color: #b53d34;
}
.pagination {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.5rem;
}
.pagination div {
  display: flex;
  gap: .5rem;
}
</style>
