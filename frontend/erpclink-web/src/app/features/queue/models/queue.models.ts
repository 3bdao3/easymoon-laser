export type QueueStatus = 'Waiting' | 'Called' | 'InService' | 'Completed' | 'Skipped' | 'Cancelled';

export type QueuePriority = 'Normal' | 'Urgent';

export interface CheckInRequest {
  appointmentId: string;
  priority: string | null;
}

export interface TodayQueueParams {
  clinicId?: string;
  doctorId?: string;
  status?: string;
  priority?: string;
}

export interface QueueListItem {
  id: string;
  queueNumber: string;
  patientId: string;
  appointmentId: string;
  doctorId: string;
  clinicId: string;
  queueDate: string;
  priority: QueuePriority;
  status: QueueStatus;
  checkInTimeUtc: string;
  calledTimeUtc: string | null;
  serviceStartTimeUtc: string | null;
  serviceEndTimeUtc: string | null;
}

export interface QueueEntry extends QueueListItem {
  organizationId: string;
  branchId: string;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
}
