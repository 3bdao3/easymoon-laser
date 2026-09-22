export type AppointmentStatus =
  | 'Scheduled'
  | 'Confirmed'
  | 'Cancelled'
  | 'NoShow'
  | 'CheckedIn';

export interface BookAppointmentRequest {
  patientId: string;
  doctorId: string;
  clinicId: string;
  date: string;
  startTime: string;
  reason: string | null;
  notes: string | null;
}

export interface CancelAppointmentRequest {
  reason: string | null;
}

export interface RescheduleAppointmentRequest {
  date: string;
  startTime: string;
}

export interface SearchAppointmentsParams {
  patientId?: string;
  doctorId?: string;
  clinicId?: string;
  date?: string;
  fromDate?: string;
  toDate?: string;
  status?: string;
  appointmentNumber?: string;
  sortBy?: string;
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
}

export interface Appointment {
  id: string;
  organizationId: string;
  branchId: string;
  appointmentNumber: string;
  patientId: string;
  doctorId: string;
  clinicId: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: AppointmentStatus;
  reason: string | null;
  notes: string | null;
  cancellationReason: string | null;
  cancelledAtUtc: string | null;
  cancelledBy: string | null;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
}

export interface AppointmentListItem {
  id: string;
  appointmentNumber: string;
  patientId: string;
  doctorId: string;
  clinicId: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: AppointmentStatus;
}

export interface PagedAppointmentsResult {
  items: AppointmentListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}
