export enum PatientDocumentCategory {
  MedicalImaging = 1,
  Laboratory = 2,
  MedicalReports = 3,
  Prescriptions = 4,
  Other = 5
}

export enum PatientDocumentType {
  XRay = 1,
  Ct = 2,
  Mri = 3,
  Ultrasound = 4,
  OtherImaging = 5,
  BloodTest = 10,
  UrineTest = 11,
  Pathology = 12,
  OtherLaboratory = 13,
  MedicalReport = 20,
  HospitalReport = 21,
  DischargeReport = 22,
  ReferralReport = 23,
  ExternalPrescription = 30,
  PreviousPrescription = 31,
  InsuranceDocument = 40,
  PatientDocument = 41,
  Other = 99
}

export const PATIENT_DOCUMENT_CATEGORY_LABELS: Record<PatientDocumentCategory, string> = {
  [PatientDocumentCategory.MedicalImaging]: 'تصوير طبي',
  [PatientDocumentCategory.Laboratory]: 'مختبر',
  [PatientDocumentCategory.MedicalReports]: 'تقارير طبية',
  [PatientDocumentCategory.Prescriptions]: 'وصفات',
  [PatientDocumentCategory.Other]: 'أخرى'
};

export const PATIENT_DOCUMENT_TYPE_LABELS: Record<PatientDocumentType, string> = {
  [PatientDocumentType.XRay]: 'أشعة سينية',
  [PatientDocumentType.Ct]: 'أشعة مقطعية',
  [PatientDocumentType.Mri]: 'رنين مغناطيسي',
  [PatientDocumentType.Ultrasound]: 'موجات فوق صوتية',
  [PatientDocumentType.OtherImaging]: 'تصوير آخر',
  [PatientDocumentType.BloodTest]: 'تحليل دم',
  [PatientDocumentType.UrineTest]: 'تحليل بول',
  [PatientDocumentType.Pathology]: 'باثولوجيا',
  [PatientDocumentType.OtherLaboratory]: 'مختبر آخر',
  [PatientDocumentType.MedicalReport]: 'تقرير طبي',
  [PatientDocumentType.HospitalReport]: 'تقرير مستشفى',
  [PatientDocumentType.DischargeReport]: 'تقرير خروج',
  [PatientDocumentType.ReferralReport]: 'تقرير تحويل',
  [PatientDocumentType.ExternalPrescription]: 'وصفة خارجية',
  [PatientDocumentType.PreviousPrescription]: 'وصفة سابقة',
  [PatientDocumentType.InsuranceDocument]: 'مستند تأمين',
  [PatientDocumentType.PatientDocument]: 'مستند مريض',
  [PatientDocumentType.Other]: 'أخرى'
};

const DOCUMENT_TYPE_VALUES = Object.values(PatientDocumentType).filter(
  (v): v is PatientDocumentType => typeof v === 'number'
);

export function categoryForDocumentType(type: PatientDocumentType): PatientDocumentCategory {
  switch (type) {
    case PatientDocumentType.XRay:
    case PatientDocumentType.Ct:
    case PatientDocumentType.Mri:
    case PatientDocumentType.Ultrasound:
    case PatientDocumentType.OtherImaging:
      return PatientDocumentCategory.MedicalImaging;
    case PatientDocumentType.BloodTest:
    case PatientDocumentType.UrineTest:
    case PatientDocumentType.Pathology:
    case PatientDocumentType.OtherLaboratory:
      return PatientDocumentCategory.Laboratory;
    case PatientDocumentType.MedicalReport:
    case PatientDocumentType.HospitalReport:
    case PatientDocumentType.DischargeReport:
    case PatientDocumentType.ReferralReport:
      return PatientDocumentCategory.MedicalReports;
    case PatientDocumentType.ExternalPrescription:
    case PatientDocumentType.PreviousPrescription:
      return PatientDocumentCategory.Prescriptions;
    default:
      return PatientDocumentCategory.Other;
  }
}

export function documentTypesForCategory(
  category: PatientDocumentCategory
): PatientDocumentType[] {
  return DOCUMENT_TYPE_VALUES.filter((t) => categoryForDocumentType(t) === category);
}

export interface PatientDocumentDto {
  id: string;
  patientId: string;
  medicalVisitId: string | null;
  category: PatientDocumentCategory;
  documentType: PatientDocumentType;
  originalFileName: string;
  fileExtension: string;
  contentType: string;
  fileSizeBytes: number;
  documentDate: string | null;
  description: string | null;
  isActive: boolean;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
}

export interface PatientDocumentCategoryCountDto {
  category: PatientDocumentCategory;
  count: number;
}

export interface PatientDocumentSummaryDto {
  totalActive: number;
  byCategory: PatientDocumentCategoryCountDto[];
}

export interface UploadPatientDocumentResult {
  document: PatientDocumentDto;
  possibleDuplicateWarning: boolean;
}

export interface UpdatePatientDocumentMetadataRequest {
  category: PatientDocumentCategory;
  documentType: PatientDocumentType;
  documentDate: string | null;
  description: string | null;
  medicalVisitId: string | null;
}

export interface SearchPatientDocumentsParams {
  query?: string;
  category?: PatientDocumentCategory;
  documentType?: PatientDocumentType;
  dateFrom?: string;
  dateTo?: string;
  isActive?: boolean;
  uploadedBy?: string;
  page?: number;
  pageSize?: number;
}

export function formatFileSizeBytes(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) {
    return '—';
  }
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}

export function isImageContentType(contentType: string): boolean {
  return contentType.startsWith('image/');
}

export function isPdfContentType(contentType: string, fileName?: string): boolean {
  if (contentType === 'application/pdf') {
    return true;
  }
  if (fileName?.toLowerCase().endsWith('.pdf')) {
    return true;
  }
  return false;
}
