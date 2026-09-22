import { SimplePagedResult } from '../../../shared/models/api.models';

export interface RegisterDoctorRequest {
  firstName: string;
  lastName: string;
  displayName?: string | null;
  specialtyId?: string | null;
  licenseNumber?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
  userId?: string | null;
}

export interface UpdateDoctorRequest {
  firstName: string;
  lastName: string;
  displayName?: string | null;
  specialtyId?: string | null;
  licenseNumber?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
  userId?: string | null;
}

export interface SearchDoctorsParams {
  query?: string;
  doctorNumber?: string;
  name?: string;
  phone?: string;
  email?: string;
  specialtyId?: string;
  clinicId?: string;
  isActive?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
}

export interface AssignDoctorToClinicRequest {
  clinicId: string;
  startDate: string;
  endDate?: string | null;
}

export interface DoctorDto {
  id: string;
  organizationId: string;
  branchId: string;
  doctorNumber: string;
  userId: string | null;
  firstName: string;
  lastName: string;
  displayName: string;
  specialtyId: string | null;
  specialtyName: string | null;
  licenseNumber: string | null;
  phoneNumber: string | null;
  email: string | null;
  isActive: boolean;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
}

export interface DoctorListItemDto {
  id: string;
  doctorNumber: string;
  displayName: string;
  specialtyId: string | null;
  specialtyName: string | null;
  phoneNumber: string | null;
  email: string | null;
  isActive: boolean;
}

export interface DoctorClinicAssignmentDto {
  id: string;
  doctorId: string;
  clinicId: string;
  clinicCode: string;
  clinicName: string;
  startDate: string;
  endDate: string | null;
  isActive: boolean;
}

export type PagedDoctorsResult = SimplePagedResult<DoctorListItemDto>;
