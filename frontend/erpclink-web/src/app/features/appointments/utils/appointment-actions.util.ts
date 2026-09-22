import { AppointmentStatus } from '../models/appointment.models';

export interface AppointmentActionFlags {
  canConfirm: boolean;
  canCancel: boolean;
  canReschedule: boolean;
  canNoShow: boolean;
}

export function appointmentActionFlags(status: AppointmentStatus): AppointmentActionFlags {
  return {
    canConfirm: status === 'Scheduled',
    canCancel: status === 'Scheduled' || status === 'Confirmed',
    canReschedule: status === 'Scheduled' || status === 'Confirmed',
    canNoShow: status === 'Confirmed'
  };
}

export const APPOINTMENT_STATUS_OPTIONS: readonly { value: AppointmentStatus | ''; label: string }[] = [
  { value: '', label: '— الكل —' },
  { value: 'Scheduled', label: 'مجدول' },
  { value: 'Confirmed', label: 'مؤكد' },
  { value: 'CheckedIn', label: 'تم الحضور' },
  { value: 'Cancelled', label: 'ملغى' },
  { value: 'NoShow', label: 'لم يحضر' }
];
