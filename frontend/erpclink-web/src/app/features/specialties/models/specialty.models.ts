export interface CreateSpecialtyRequest {
  code: string;
  name: string;
  description?: string | null;
}

export interface UpdateSpecialtyRequest {
  name: string;
  description?: string | null;
}

export interface SpecialtyDto {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
}
