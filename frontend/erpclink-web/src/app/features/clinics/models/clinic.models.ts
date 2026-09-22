import { SimplePagedResult } from '../../../shared/models/api.models';

export interface CreateClinicRequest {
  code: string;
  name: string;
  description?: string | null;
  location?: string | null;
}

export interface UpdateClinicRequest {
  name: string;
  description?: string | null;
  location?: string | null;
}

export interface SearchClinicsParams {
  query?: string;
  code?: string;
  name?: string;
  branchId?: string;
  isActive?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
}

export interface ClinicDto {
  id: string;
  organizationId: string;
  branchId: string;
  code: string;
  name: string;
  description: string | null;
  location: string | null;
  isActive: boolean;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
}

export interface ClinicListItemDto {
  id: string;
  code: string;
  name: string;
  location: string | null;
  isActive: boolean;
}

export type PagedClinicsResult = SimplePagedResult<ClinicListItemDto>;
