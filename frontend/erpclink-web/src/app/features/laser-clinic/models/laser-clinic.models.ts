/** Laser Clinic API models — mirrors backend DTOs (camelCase JSON). */

export enum LaserAppointmentStatus {
  Pending = 0,
  Confirmed = 1,
  Attended = 2,
  Cancelled = 3,
  NoShow = 4
}

export const LASER_APPOINTMENT_STATUS_LABELS: Record<LaserAppointmentStatus, string> = {
  [LaserAppointmentStatus.Pending]: 'لم يتم التأكيد',
  [LaserAppointmentStatus.Confirmed]: 'مؤكد',
  [LaserAppointmentStatus.Attended]: 'حضرت',
  [LaserAppointmentStatus.Cancelled]: 'ملغي',
  [LaserAppointmentStatus.NoShow]: 'لم تحضر'
};

export const LASER_APPOINTMENT_STATUS_BADGE: Record<LaserAppointmentStatus, string> = {
  [LaserAppointmentStatus.Pending]: 'badge--pending',
  [LaserAppointmentStatus.Confirmed]: 'badge--confirmed',
  [LaserAppointmentStatus.Attended]: 'badge--attended',
  [LaserAppointmentStatus.Cancelled]: 'badge--cancelled',
  [LaserAppointmentStatus.NoShow]: 'badge--noshow'
};

export const LASER_APPOINTMENT_STATUS_BLOCK: Record<LaserAppointmentStatus, string> = {
  [LaserAppointmentStatus.Pending]: 'calendar-block--pending',
  [LaserAppointmentStatus.Confirmed]: 'calendar-block--confirmed',
  [LaserAppointmentStatus.Attended]: 'calendar-block--attended',
  [LaserAppointmentStatus.Cancelled]: 'calendar-block--cancelled',
  [LaserAppointmentStatus.NoShow]: 'calendar-block--noshow'
};

export interface CustomerPulseBalanceDto {
  packageTotal: number | null;
  remaining: number | null;
  consumed: number;
  packagePriceTotal?: number | null;
  amountPaid?: number;
  remainingAmount?: number;
  packageDurationTotalMinutes?: number | null;
  durationUsedMinutes?: number;
  remainingDurationMinutes?: number;
}

export interface CustomerDto {
  id: string;
  fullName: string;
  phoneNumber: string;
  age: number | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  pulseBalance?: CustomerPulseBalanceDto | null;
}

export interface CustomerNextAppointmentDto {
  id: string;
  date: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  status: LaserAppointmentStatus;
  serviceNames: string[];
}

export interface CustomerLastAppointmentDto {
  id: string;
  date: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  status: LaserAppointmentStatus;
  serviceNames: string[];
}

export interface CustomerListItemDto extends CustomerDto {
  nextAppointment: CustomerNextAppointmentDto | null;
  lastAppointment?: CustomerLastAppointmentDto | null;
}

export type CustomerListSort = 'Name' | 'NextAppointment' | 'Newest' | 'Oldest' | 'LastAppointment';

export interface CreateCustomerRequest {
  fullName: string;
  phoneNumber: string;
  age?: number | null;
  notes?: string | null;
}

export interface UpdateCustomerRequest {
  fullName: string;
  phoneNumber: string;
  age?: number | null;
  notes?: string | null;
}

export interface LaserServiceDto {
  id: string;
  name: string;
  minDurationMinutes: number;
  maxDurationMinutes: number;
  recommendedDurationMinutes: number;
  isActive: boolean;
  displayOrder: number;
  notes: string | null;
  requiresManualDuration?: boolean;
}

export function serviceRequiresManualDuration(s: Pick<LaserServiceDto, 'name' | 'requiresManualDuration'>): boolean {
  return s.requiresManualDuration === true || s.name.includes('نبضة');
}

export interface CreateLaserServiceRequest {
  name: string;
  minDurationMinutes: number;
  maxDurationMinutes: number;
  displayOrder: number;
  notes?: string | null;
}

export interface UpdateLaserServiceRequest {
  name: string;
  minDurationMinutes: number;
  maxDurationMinutes: number;
  displayOrder: number;
  notes?: string | null;
  isActive: boolean;
}

export interface AppointmentServiceLineDto {
  id: string;
  laserServiceId: string;
  serviceName: string;
  durationMinutes: number;
  pulsesConsumed?: number | null;
}

export interface LaserAppointmentDto {
  id: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  status: LaserAppointmentStatus;
  notes: string | null;
  services: AppointmentServiceLineDto[];
  amountPaid?: number | null;
}

export interface RecordSessionRequest {
  durationMinutes: number;
  pulsesConsumed?: number | null;
  amountPaid?: number | null;
  markAttended?: boolean;
  packagePrice?: number | null;
}

export interface SessionDetailDto {
  appointment: LaserAppointmentDto;
  pulseBalance: CustomerPulseBalanceDto;
  totalAmountPaid: number;
  availablePulses: number;
}

export interface ServiceDurationOverrideDto {
  serviceId: string;
  durationMinutes: number;
  pulsesConsumed?: number | null;
}

export interface CreateLaserAppointmentRequest {
  customerId: string;
  appointmentDate: string;
  startTime: string;
  laserServiceIds: string[];
  notes?: string | null;
  serviceDurationOverrides?: ServiceDurationOverrideDto[] | null;
}

export interface BookingCustomerInput {
  fullName: string;
  phoneNumber: string;
  age?: number | null;
  notes?: string | null;
}

export interface CreateBookingWithCustomerRequest {
  existingCustomerId?: string | null;
  customer?: BookingCustomerInput | null;
  appointmentDate: string;
  startTime: string;
  laserServiceIds: string[];
  appointmentNotes?: string | null;
  serviceDurationOverrides?: ServiceDurationOverrideDto[] | null;
}

export interface SlotCheckResultDto {
  isAvailable: boolean;
  clinicalDurationMinutes: number;
  endTime: string;
  messageAr: string;
}

export interface UpdateLaserAppointmentStatusRequest {
  status: LaserAppointmentStatus;
}

export enum AvailabilitySlotStatus {
  Available = 0,
  Booked = 1,
  Unavailable = 2
}

export interface AvailabilitySlotDto {
  startTime: string;
  endTime: string;
  durationMinutes: number;
  status: AvailabilitySlotStatus;
  appointmentId?: string | null;
  customerName?: string | null;
  serviceNames?: string | null;
  bookedDurationMinutes?: number | null;
}

export interface AvailabilityResultDto {
  date: string;
  clinicalDurationMinutes: number;
  bufferMinutes: number;
  blockedDurationMinutes: number;
  openingTime: string;
  closingTime: string;
  slotIntervalMinutes: number;
  slots: AvailabilitySlotDto[];
}

export interface CustomerHistoryDto {
  customerId: string;
  fullName: string;
  phoneNumber: string;
  upcoming: LaserAppointmentDto[];
  previous: LaserAppointmentDto[];
  pulseBalance?: CustomerPulseBalanceDto | null;
}

/** Sum of pulsesConsumed on appointment service lines; null when none. */
export function appointmentPulsesConsumed(appt: Pick<LaserAppointmentDto, 'services'>): number | null {
  let total = 0;
  let any = false;
  for (const line of appt.services ?? []) {
    if (line.pulsesConsumed != null && line.pulsesConsumed > 0) {
      total += line.pulsesConsumed;
      any = true;
    }
  }
  return any ? total : null;
}

export interface ClinicSettingsDto {
  id: string;
  openingTime: string;
  closingTime: string;
  appointmentSlotIntervalMinutes: number;
  defaultBufferMinutes: number;
  isActive: boolean;
}

export interface UpdateClinicSettingsRequest {
  openingTime: string;
  closingTime: string;
  appointmentSlotIntervalMinutes: number;
  defaultBufferMinutes: number;
}

export interface LaserDashboardDto {
  date: string;
  totalAppointments: number;
  pendingCount: number;
  confirmedCount: number;
  attendedCount: number;
  cancelledCount: number;
  noShowCount: number;
  remainingCount: number;
  occupiedMinutes: number;
  availableMinutes: number;
  pulsesToday: number;
  pulsesThisMonth: number;
  pulsesAllTime: number;
  todaysAppointments: LaserAppointmentDto[];
  upcoming: LaserAppointmentDto[];
}

/** Format TimeOnly-style string for display (HH:mm). */
export function formatTime(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }
  return value.length >= 5 ? value.slice(0, 5) : value;
}

/** Format time as Arabic 12h style: 05:00 م */
export function formatTimeAr(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }
  const raw = value.length >= 5 ? value.slice(0, 5) : value;
  const [hs, ms] = raw.split(':');
  let h = Number(hs);
  const m = ms ?? '00';
  const suffix = h >= 12 ? 'م' : 'ص';
  h = h % 12;
  if (h === 0) {
    h = 12;
  }
  return `${String(h).padStart(2, '0')}:${m} ${suffix}`;
}

const ARABIC_MONTHS = [
  'يناير',
  'فبراير',
  'مارس',
  'أبريل',
  'مايو',
  'يونيو',
  'يوليو',
  'أغسطس',
  'سبتمبر',
  'أكتوبر',
  'نوفمبر',
  'ديسمبر'
];

export interface LaserOfferDto {
  id: string;
  title: string;
  description: string | null;
  price: number;
  validFrom: string | null;
  validTo: string | null;
  isActive: boolean;
  displayOrder: number;
  createdAtUtc: string;
}

export interface CreateLaserOfferRequest {
  title: string;
  description?: string | null;
  price: number;
  validFrom?: string | null;
  validTo?: string | null;
  displayOrder: number;
}

export interface UpdateLaserOfferRequest {
  title: string;
  description?: string | null;
  price: number;
  validFrom?: string | null;
  validTo?: string | null;
  displayOrder: number;
  isActive: boolean;
}

/** Format ISO date yyyy-MM-dd as Arabic friendly: 22 سبتمبر 2026 */
export function formatDateAr(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }
  const part = value.slice(0, 10);
  const [y, m, d] = part.split('-').map(Number);
  if (!y || !m || !d) {
    return part;
  }
  return `${d} ${ARABIC_MONTHS[m - 1]} ${y}`;
}

export function relativeDayLabel(isoDate: string, todayIso?: string): 'today' | 'tomorrow' | null {
  const today = todayIso ?? new Date().toISOString().slice(0, 10);
  const t = new Date(today + 'T12:00:00');
  const d = new Date(isoDate.slice(0, 10) + 'T12:00:00');
  const diff = Math.round((d.getTime() - t.getTime()) / 86400000);
  if (diff === 0) {
    return 'today';
  }
  if (diff === 1) {
    return 'tomorrow';
  }
  return null;
}

/** Normalize time input (HH:mm) to API TimeOnly (HH:mm:ss). */
export function toApiTime(value: string): string {
  if (!value) {
    return value;
  }
  return value.length === 5 ? `${value}:00` : value;
}
