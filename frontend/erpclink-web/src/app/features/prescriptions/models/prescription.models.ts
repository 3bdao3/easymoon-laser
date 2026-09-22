export type PrescriptionStatus = 'Draft' | 'Issued' | 'Cancelled';

export interface PrescriptionItemDto {
  id: string;
  medicationId: string;
  medicationNameSnapshot: string;
  dosage: string;
  frequency: string;
  duration: string | null;
  route: string | null;
  instructions: string | null;
  quantity: number | null;
  notes: string | null;
  sortOrder: number;
}

export interface PrescriptionDto {
  id: string;
  organizationId: string;
  branchId: string;
  prescriptionNumber: string;
  medicalVisitId: string;
  patientId: string;
  doctorId: string;
  prescriptionDate: string;
  status: PrescriptionStatus;
  notes: string | null;
  issuedAtUtc: string | null;
  issuedBy: string | null;
  cancelledAtUtc: string | null;
  cancelledBy: string | null;
  cancellationReason: string | null;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
  rowVersion: string;
  items: PrescriptionItemDto[];
}

export interface PrescriptionListItemDto {
  id: string;
  prescriptionNumber: string;
  prescriptionDate: string;
  patientId: string;
  doctorId: string;
  medicalVisitId: string;
  status: PrescriptionStatus;
  itemCount: number;
}

export interface PagedPrescriptionsResult {
  items: PrescriptionListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface PrescriptionItemInput {
  medicationId: string;
  dosage: string;
  frequency: string;
  duration?: string | null;
  route?: string | null;
  instructions?: string | null;
  quantity?: number | null;
  notes?: string | null;
  sortOrder?: number | null;
}

export interface CreatePrescriptionRequest {
  medicalVisitId: string;
  notes?: string | null;
  items?: PrescriptionItemInput[] | null;
}

export interface UpdatePrescriptionRequest {
  notes?: string | null;
  rowVersion?: string | null;
}

export interface UpdatePrescriptionItemRequest {
  dosage: string;
  frequency: string;
  duration?: string | null;
  route?: string | null;
  instructions?: string | null;
  quantity?: number | null;
  notes?: string | null;
  sortOrder: number;
  rowVersion?: string | null;
}

export interface CancelPrescriptionRequest {
  reason?: string | null;
}

export interface SearchPrescriptionsParams {
  patientId?: string;
  doctorId?: string;
  medicalVisitId?: string;
  prescriptionNumber?: string;
  dateFrom?: string;
  dateTo?: string;
  status?: PrescriptionStatus | '';
  page?: number;
  pageSize?: number;
}

export interface PatientPrescriptionHistoryParams {
  dateFrom?: string;
  dateTo?: string;
  status?: PrescriptionStatus | '';
  page?: number;
  pageSize?: number;
}

export function isPrescriptionDraft(status: PrescriptionStatus): boolean {
  return status === 'Draft';
}

export function isPrescriptionEditable(status: PrescriptionStatus | string): boolean {
  return status === 'Draft';
}

export function canIssuePrescription(status: PrescriptionStatus | string, itemCount: number): boolean {
  return status === 'Draft' && itemCount > 0;
}

export function canCancelPrescription(status: PrescriptionStatus | string): boolean {
  return status === 'Draft' || status === 'Issued';
}
