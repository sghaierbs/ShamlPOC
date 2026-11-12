export interface CenterDto {
  id: number
  name: string
  displayName: string
  regionId: number
  cityId: number
  districtId: number | null
  operationalCapacity: number
  waitingListLimit: number
  directorSocialId: string | null
  isActive: boolean
  isReceivingCases: boolean
  createdAtUtc: string
}

export interface GeographicLookup {
  id: number
  nameAr: string
  nameEn: string
}

export interface CenterFormModel {
  name: string
  regionId: number | null
  cityId: number | null
  districtId: number | null
  operationalCapacity: number
  waitingListLimit: number
  directorSocialId: string
  isActive: boolean
  isReceivingCases: boolean
}
