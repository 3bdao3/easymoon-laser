export enum Gender {
  Unknown = 0,
  Male = 1,
  Female = 2,
  Other = 3
}

export enum AllergySeverity {
  Unknown = 0,
  Mild = 1,
  Moderate = 2,
  Severe = 3,
  LifeThreatening = 4
}

export interface RegisterPatientRequest {
  firstName: string;
  middleName?: string | null;
  lastName: string;
  dateOfBirth: string;
  gender: Gender;
  nationalId?: string | null;
  phoneNumber: string;
  email?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  allowPossibleDuplicate?: boolean;
}

export interface UpdatePatientRequest {
  firstName: string;
  middleName?: string | null;
  lastName: string;
  dateOfBirth: string;
  gender: Gender;
  nationalId?: string | null;
  phoneNumber: string;
  email?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
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

export interface AddAllergyRequest {
  name: string;
  reaction?: string | null;
  severity: AllergySeverity;
  notes?: string | null;
}

export interface UpdateAllergyRequest {
  name: string;
  reaction?: string | null;
  severity: AllergySeverity;
  notes?: string | null;
}

export interface AllergyDto {
  id: string;
  name: string;
  reaction: string | null;
  severity: AllergySeverity;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface PatientDto {
  id: string;
  organizationId: string;
  branchId: string;
  patientNumber: string;
  firstName: string;
  middleName: string | null;
  lastName: string;
  fullName: string;
  dateOfBirth: string;
  gender: Gender;
  nationalId: string | null;
  phoneNumber: string;
  email: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  isActive: boolean;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
  allergies: AllergyDto[];
  medicalHistory: MedicalHistoryItemDto[];
}

export interface MedicalHistoryItemDto {
  id: string;
  category: string;
  description: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface PatientListItemDto {
  id: string;
  patientNumber: string;
  fullName: string;
  dateOfBirth: string;
  phoneNumber: string;
  nationalId: string | null;
  isActive: boolean;
}

export interface RegisterPatientResult {
  patient: PatientDto;
  possibleDuplicates: PatientListItemDto[];
}

export const GENDER_LABELS: Record<Gender, string> = {
  [Gender.Unknown]: 'غير محدد',
  [Gender.Male]: 'ذكر',
  [Gender.Female]: 'أنثى',
  [Gender.Other]: 'أخرى'
};

export const ALLERGY_SEVERITY_LABELS: Record<AllergySeverity, string> = {
  [AllergySeverity.Unknown]: 'غير محدد',
  [AllergySeverity.Mild]: 'خفيف',
  [AllergySeverity.Moderate]: 'متوسط',
  [AllergySeverity.Severe]: 'شديد',
  [AllergySeverity.LifeThreatening]: 'مهدد للحياة'
};
