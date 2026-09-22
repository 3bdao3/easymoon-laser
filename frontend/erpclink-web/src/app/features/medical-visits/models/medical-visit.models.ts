export type MedicalVisitStatus = 'Open' | 'InProgress' | 'Completed' | 'Cancelled';

export interface MedicalVisitDto {
  id: string;
  organizationId: string;
  branchId: string;
  visitNumber: string;
  patientId: string;
  appointmentId: string;
  doctorId: string;
  clinicId: string;
  queueEntryId: string | null;
  visitDate: string;
  status: MedicalVisitStatus;
  chiefComplaint: string | null;
  clinicalNotes: string | null;
  examinationFindings: string | null;
  diagnosisNotes: string | null;
  followUpNotes: string | null;
  completedAtUtc: string | null;
  completedBy: string | null;
  cancelledAtUtc: string | null;
  cancelledBy: string | null;
  cancellationReason: string | null;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
  rowVersion: string;
}

export interface MedicalVisitListItemDto {
  id: string;
  visitNumber: string;
  visitDate: string;
  patientId: string;
  doctorId: string;
  clinicId: string;
  appointmentId: string;
  status: MedicalVisitStatus;
  chiefComplaint: string | null;
  diagnosisNotes: string | null;
  followUpNotes: string | null;
}

export interface PagedMedicalVisitsResult {
  items: MedicalVisitListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface StartMedicalVisitRequest {
  appointmentId: string;
  queueEntryId?: string | null;
}

export interface UpdateClinicalNotesRequest {
  chiefComplaint?: string | null;
  clinicalNotes?: string | null;
  examinationFindings?: string | null;
  diagnosisNotes?: string | null;
  followUpNotes?: string | null;
  rowVersion?: string | null;
}

export interface CancelMedicalVisitRequest {
  reason?: string | null;
}

export interface SearchMedicalVisitsParams {
  patientId?: string;
  doctorId?: string;
  clinicId?: string;
  appointmentId?: string;
  visitDate?: string;
  dateFrom?: string;
  dateTo?: string;
  status?: MedicalVisitStatus | '';
  visitNumber?: string;
  page?: number;
  pageSize?: number;
}

export interface PatientVisitHistoryParams {
  dateFrom?: string;
  dateTo?: string;
  doctorId?: string;
  clinicId?: string;
  status?: MedicalVisitStatus | '';
  page?: number;
  pageSize?: number;
}

export function isMedicalVisitNotesEditable(status: MedicalVisitStatus): boolean {
  return status === 'Open' || status === 'InProgress';
}

/** Derived Patient 360 visit context (not a persisted patient status). */
export type PatientRelationshipCode = 'New' | 'Returning';
export type PatientTreatmentStateCode = 'None' | 'InTreatment' | 'TreatmentCompleted';
export type CurrentEncounterKindCode = 'FirstVisit' | 'FollowUp' | 'Consultation';

export interface PatientVisitContextDto {
  totalVisits: number;
  completedVisits: number;
  activeVisits: number;
  isReturningPatient: boolean;
  patientRelationship: PatientRelationshipCode;
  treatmentState: PatientTreatmentStateCode;
  currentEncounterKind: CurrentEncounterKindCode | null;
  activeVisit: MedicalVisitListItemDto | null;
  lastVisit: MedicalVisitListItemDto | null;
  previousVisitHadFollowUpNotes: boolean;
}

export const PATIENT_RELATIONSHIP_LABELS: Record<PatientRelationshipCode, string> = {
  New: 'مريض جديد',
  Returning: 'مريض عائد'
};

export const PATIENT_TREATMENT_STATE_LABELS: Record<PatientTreatmentStateCode, string> = {
  None: 'لا توجد زيارات بعد',
  InTreatment: 'قيد العلاج',
  TreatmentCompleted: 'اكتمل العلاج'
};

export const CURRENT_ENCOUNTER_KIND_LABELS: Record<CurrentEncounterKindCode, string> = {
  FirstVisit: 'زيارة أولى',
  FollowUp: 'متابعة',
  Consultation: 'استشارة'
};
