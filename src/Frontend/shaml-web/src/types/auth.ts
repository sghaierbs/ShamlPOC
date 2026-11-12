export interface UserRoleResponse {
  userRoleId: number
  roleId: number
  roleCode: string
  roleName: string
  centerId: number | null
  centerDisplayName: string | null
  isDefault: boolean
  isActive: boolean
}

export interface LoginResponse {
  accessToken: string
  tokenType: string
  expiresIn: number
  activeUserRoleId: number
}

export function getRoleScope(role: UserRoleResponse): string {
  return role.centerDisplayName ?? 'Organization / Public'
}
