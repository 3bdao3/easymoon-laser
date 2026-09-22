export interface CreateDoctorScheduleRequest {
  doctorId: string;
  clinicId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
  effectiveFrom: string;
  effectiveTo: string | null;
}

export interface UpdateDoctorScheduleRequest {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
  effectiveFrom: string;
  effectiveTo: string | null;
}

export interface DoctorSchedule {
  id: string;
  organizationId: string;
  branchId: string;
  doctorId: string;
  clinicId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo: string | null;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
}

export interface AvailabilitySlot {
  start: string;
  end: string;
}

export interface DoctorAvailability {
  doctorId: string;
  clinicId: string | null;
  date: string;
  isHoliday: boolean;
  holidayName: string | null;
  isDoctorUnavailable: boolean;
  slots: AvailabilitySlot[];
}
