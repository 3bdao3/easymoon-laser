/** Appointment booking step validation — UI orchestration only. */

export interface AppointmentBookingDraft {
  patientId: string | null;
  doctorId: string | null;
  clinicId: string | null;
  date: string | null;
  startTime: string | null;
}

export function canLoadAvailability(draft: AppointmentBookingDraft): boolean {
  return !!draft.doctorId && !!draft.date;
}

export function canSubmitAppointment(draft: AppointmentBookingDraft): boolean {
  return (
    !!draft.patientId &&
    !!draft.doctorId &&
    !!draft.clinicId &&
    !!draft.date &&
    !!draft.startTime
  );
}
