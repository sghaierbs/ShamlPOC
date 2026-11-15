
<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import type {
  CaseTask,
  PagedTasksResponse,
  TaskStatus,
} from '../../types/tasks'

const props = defineProps<{
  accessToken: string
}>()

const tasks = ref<CaseTask[]>([])
const loading = ref(false)
const errorMessage = ref('')

const search = ref('')
const status = ref('')
const page = ref(1)
const pageSize = 20
const totalCount = ref(0)

const totalPages = computed(() =>
    Math.max(1, Math.ceil(totalCount.value / pageSize)),
)

const statusOptions = [
  { value: '', label: 'All statuses' },
  { value: 'PENDING', label: 'Pending' },
  { value: 'IN_PROGRESS', label: 'In Progress' },
  { value: 'COMPLETED', label: 'Completed' },
  { value: 'CANCELLED', label: 'Cancelled' },
]

let currentRequest: AbortController | null = null

async function loadTasks() {
  currentRequest?.abort()

  const controller = new AbortController()
  currentRequest = controller

  loading.value = true
  errorMessage.value = ''

  const params = new URLSearchParams({
    page: String(page.value),
    pageSize: String(pageSize),
  })

  if (status.value) {
    params.set('status', status.value)
  }

  if (search.value.trim()) {
    params.set('search', search.value.trim())
  }

  try {
    const response = await fetch(
        `/api/v1/tasks/my?${params.toString()}`,
        {
          method: 'GET',
          headers: {
            Authorization: `Bearer ${props.accessToken}`,
          },
          signal: controller.signal,
        },
    )

    if (!response.ok) {
      throw new Error(
          `Unable to load tasks (HTTP ${response.status}).`,
      )
    }

    const data =
        (await response.json()) as PagedTasksResponse

    if (currentRequest !== controller) return

    tasks.value = data.items
    totalCount.value = data.totalCount
  } catch (error) {
    if (controller.signal.aborted) return

    tasks.value = []
    totalCount.value = 0

    errorMessage.value =
        error instanceof Error
            ? error.message
            : 'Unable to load tasks.'
  } finally {
    if (currentRequest === controller) {
      loading.value = false
      currentRequest = null
    }
  }
}

function applyFilters() {
  page.value = 1
  void loadTasks()
}

function changePage(nextPage: number) {
  if (nextPage < 1 || nextPage > totalPages.value) return

  page.value = nextPage
  void loadTasks()
}

function formatDate(value: string) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) return value

  return new Intl.DateTimeFormat('en-GB', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date)
}

function statusLabel(value: TaskStatus) {
  return statusOptions.find(
      option => option.value === value,
  )?.label ?? value
}

watch(
    () => props.accessToken,
    () => {
      page.value = 1
      tasks.value = []
      void loadTasks()
    },
)

onMounted(() => {
  void loadTasks()
})
</script>

<template>
  <section class="page-heading">
    <div>
      <nav>Home <span>›</span> My Tasks</nav>
      <h1>My Tasks</h1>
      <p>
        View and manage tasks assigned to your active center role.
      </p>
    </div>
  </section>

  <main class="content">
    <section class="card">
      <header class="card-header">
        <div>
          <span class="eyebrow">TASK MANAGEMENT</span>
          <h2>Assigned tasks</h2>
        </div>

        <button
            class="refresh-button"
            type="button"
            :disabled="loading"
            @click="loadTasks"
        >
          Refresh
        </button>
      </header>

      <form class="filters" @submit.prevent="applyFilters">
        <label>
          Search
          <input
              v-model="search"
              placeholder="Case number or task title"
          />
        </label>

        <label>
          Status
          <select v-model="status">
            <option
                v-for="option in statusOptions"
                :key="option.value"
                :value="option.value"
            >
              {{ option.label }}
            </option>
          </select>
        </label>

        <button type="submit" :disabled="loading">
          Apply Filters
        </button>
      </form>

      <div v-if="errorMessage" class="error-state">
        <strong>Unable to load tasks</strong>
        <p>{{ errorMessage }}</p>
        <button type="button" @click="loadTasks">
          Retry
        </button>
      </div>

      <div v-else-if="loading" class="empty-state">
        Loading tasks...
      </div>

      <div v-else-if="tasks.length === 0" class="empty-state">
        <h3>No tasks found</h3>
        <p>
          There are no tasks matching your current filters.
        </p>
      </div>

      <template v-else>
        <div class="table-wrapper">
          <table>
            <thead>
            <tr>
              <th>Case Number</th>
              <th>Task</th>
              <th>Status</th>
              <th>Created At</th>
            </tr>
            </thead>

            <tbody>
            <tr v-for="task in tasks" :key="task.id">
              <td class="case-number">
                {{ task.caseNumber }}
              </td>

              <td>
                <strong>{{ task.title }}</strong>
                <small>{{ task.taskType }}</small>
              </td>

              <td>
                  <span
                      class="task-status"
                      :class="task.status.toLowerCase()"
                  >
                    {{ statusLabel(task.status) }}
                  </span>
              </td>

              <td>
                {{ formatDate(task.createdAtUtc) }}
              </td>
            </tr>
            </tbody>
          </table>
        </div>

        <footer class="pagination">
          <span>
            {{ totalCount }} tasks · Page {{ page }}
            of {{ totalPages }}
          </span>

          <div>
            <button
                type="button"
                :disabled="page <= 1 || loading"
                @click="changePage(page - 1)"
            >
              Previous
            </button>

            <button
                type="button"
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
  margin: 0 auto;
  max-width: 88rem;
  padding: 2rem 4rem;
}

.page-heading nav {
  color: var(--muted);
  font-size: .78rem;
  margin-bottom: 1.4rem;
}

.page-heading nav span {
  margin: 0 .5rem;
}

.page-heading h1 {
  color: var(--ink);
  font-size: 2.5rem;
  margin: 0;
}

.page-heading p {
  color: var(--muted);
  margin: .35rem 0 0;
}

.card {
  background: white;
  border: 1px solid var(--border);
  border-radius: .5rem;
  overflow: hidden;
}

.card-header {
  align-items: center;
  background: var(--mint);
  border-bottom: 1px solid var(--border);
  display: flex;
  justify-content: space-between;
  padding: 1.3rem 1.6rem;
}

.eyebrow {
  color: var(--brand);
  font-size: .7rem;
  font-weight: 750;
  letter-spacing: .13em;
}

.card h2 {
  color: var(--ink);
  font-size: 1.15rem;
  margin: .2rem 0 0;
}

.filters {
  align-items: end;
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  padding: 1.5rem;
}

.filters label {
  color: var(--ink);
  display: grid;
  flex: 1;
  font-size: .85rem;
  font-weight: 650;
  gap: .4rem;
  min-width: 12rem;
}

.filters input,
.filters select {
  background: white;
  border: 1px solid var(--border-strong);
  border-radius: .3rem;
  color: var(--ink);
  min-height: 2.8rem;
  padding: .55rem .75rem;
}

button {
  background: white;
  border: 1px solid var(--border-strong);
  border-radius: .3rem;
  color: var(--ink);
  cursor: pointer;
  min-height: 2.8rem;
  padding: .55rem 1rem;
}

.filters button {
  background: var(--brand);
  border-color: var(--brand);
  color: white;
  font-weight: 650;
}

button:disabled {
  cursor: not-allowed;
  opacity: .5;
}

.table-wrapper {
  overflow-x: auto;
}

table {
  border-collapse: collapse;
  text-align: left;
  width: 100%;
}

th,
td {
  border-bottom: 1px solid var(--border);
  padding: 1rem 1.5rem;
}

th {
  background: #f8faf9;
  color: var(--muted);
  font-size: .78rem;
  font-weight: 700;
}

td {
  color: var(--ink);
  font-size: .9rem;
}

td small {
  color: var(--muted);
  display: block;
  margin-top: .25rem;
}

.case-number {
  color: var(--brand);
  font-weight: 700;
}

.task-status {
  border-radius: 999px;
  display: inline-block;
  font-size: .75rem;
  font-weight: 700;
  padding: .35rem .7rem;
}

.task-status.pending {
  background: #fff2d5;
  color: #916200;
}

.task-status.in_progress {
  background: #dcecff;
  color: #245b9b;
}

.task-status.completed {
  background: #dff3eb;
  color: #116b4a;
}

.task-status.cancelled {
  background: #f1f1f1;
  color: #666;
}

.pagination {
  align-items: center;
  display: flex;
  justify-content: space-between;
  padding: 1rem 1.5rem;
}

.pagination div {
  display: flex;
  gap: .5rem;
}

.empty-state,
.error-state {
  color: var(--muted);
  padding: 4rem 2rem;
  text-align: center;
}

.error-state {
  color: #b53d34;
}

@media (max-width: 700px) {
  .page-heading > div,
  .content {
    padding: 1.5rem;
  }

  .pagination {
    align-items: flex-start;
    flex-direction: column;
    gap: 1rem;
  }
}
</style>
