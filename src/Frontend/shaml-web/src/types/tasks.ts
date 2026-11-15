
export type TaskStatus =
    | 'PENDING'
    | 'IN_PROGRESS'
    | 'COMPLETED'
    | 'CANCELLED'

export interface CaseTask {
    id: string
    caseId: string
    caseNumber: string
    taskType: string
    title: string
    assignedUserRoleId: number
    status: TaskStatus
    createdAtUtc: string
    startedAtUtc?: string | null
    completedAtUtc?: string | null
}

export interface PagedTasksResponse {
    items: CaseTask[]
    page: number
    pageSize: number
    totalCount: number
}
