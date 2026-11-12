export interface ManagedUserRole {
  userRoleId: number
  roleId: number
  roleCode: string
  roleName: string
  roleScope: number
  centerId: number | null
  centerDisplayName: string | null
  isDefault: boolean
  assignedAtUtc: string
}

export interface ManagedUser {
  socialId: string
  username: string
  fullName: string
  isActive: boolean
  createdAtUtc: string
  roles: ManagedUserRole[]
}

export interface AvailableRole {
  roleId: number
  roleCode: string
  roleName: string
  roleScope: number
}
