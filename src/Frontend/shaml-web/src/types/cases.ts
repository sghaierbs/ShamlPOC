
export interface AssignedCase {
    id: string
    caseNumber: string
    status: string
    assignedAtUtc: string
    createdAtUtc: string
}

export interface PagedCasesResponse {
    items: AssignedCase[]
    page: number
    pageSize: number
    totalCount: number
}
