export interface DoctorDetail {
  id: string;
  doctorNumber: string;
  displayName: string;
  firstName: string;
  lastName: string;
  specialtyId: string | null;
  specialtyName: string | null;
  isActive: boolean;
}

export interface ClinicDetail {
  id: string;
  code: string;
  name: string;
  location: string | null;
  isActive: boolean;
}

export interface DoctorListItem {
  id: string;
  doctorNumber: string;
  displayName: string;
  specialtyId: string | null;
  specialtyName: string | null;
  phoneNumber: string | null;
  email: string | null;
  isActive: boolean;
}

export interface PagedDoctorsResult {
  items: DoctorListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface PatientListItem {
  id: string;
  patientNumber: string;
  fullName: string;
  dateOfBirth: string;
  phoneNumber: string;
  nationalId: string | null;
  isActive: boolean;
}

export interface PagedPatientsResult {
  items: PatientListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ClinicListItem {
  id: string;
  code: string;
  name: string;
  location: string | null;
  isActive: boolean;
}

export interface PagedClinicsResult {
  items: ClinicListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
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

export interface SearchPatientsParams {
  query?: string;
  patientNumber?: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  nationalId?: string;
  isActive?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
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
