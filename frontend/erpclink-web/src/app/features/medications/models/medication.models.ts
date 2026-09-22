export interface MedicationDto {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  genericName: string | null;
  strength: string | null;
  dosageForm: string | null;
  route: string | null;
  isActive: boolean;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
}

export interface PagedMedicationsResult {
  items: MedicationDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateMedicationRequest {
  code: string;
  name: string;
  genericName?: string | null;
  strength?: string | null;
  dosageForm?: string | null;
  route?: string | null;
}

export interface UpdateMedicationRequest {
  name: string;
  genericName?: string | null;
  strength?: string | null;
  dosageForm?: string | null;
  route?: string | null;
}

export interface SearchMedicationsParams {
  q?: string;
  code?: string;
  name?: string;
  genericName?: string;
  strength?: string;
  dosageForm?: string;
  activeOnly?: boolean;
  page?: number;
  pageSize?: number;
}
