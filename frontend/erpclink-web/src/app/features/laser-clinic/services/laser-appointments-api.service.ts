import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AvailabilityResultDto,
  CreateBookingWithCustomerRequest,
  CreateLaserAppointmentRequest,
  LaserAppointmentDto,
  LaserAppointmentStatus,
  RecordSessionRequest,
  SessionDetailDto,
  SlotCheckResultDto,
  UpdateLaserAppointmentStatusRequest
} from '../models/laser-clinic.models';

@Injectable({ providedIn: 'root' })
export class LaserAppointmentsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/laser-appointments`;

  list(date?: string, customerId?: string): Observable<LaserAppointmentDto[]> {
    let params = new HttpParams();
    if (date) {
      params = params.set('date', date);
    }
    if (customerId) {
      params = params.set('customerId', customerId);
    }
    return this.http.get<LaserAppointmentDto[]>(this.base, { params });
  }

  getById(id: string): Observable<LaserAppointmentDto> {
    return this.http.get<LaserAppointmentDto>(`${this.base}/${id}`);
  }

  getAvailability(
    date: string,
    serviceIds: string[],
    excludeAppointmentId?: string,
    durationOverrides?: Record<string, number>
  ): Observable<AvailabilityResultDto> {
    let params = new HttpParams().set('date', date);
    for (const id of serviceIds) {
      params = params.append('serviceIds', id);
    }
    if (excludeAppointmentId) {
      params = params.set('excludeAppointmentId', excludeAppointmentId);
    }
    if (durationOverrides) {
      for (const [serviceId, minutes] of Object.entries(durationOverrides)) {
        params = params.append('durationOverrides', `${serviceId}:${minutes}`);
      }
    }
    return this.http.get<AvailabilityResultDto>(`${this.base}/availability`, { params });
  }

  checkSlot(date: string, startTime: string, serviceIds: string[]): Observable<SlotCheckResultDto> {
    let params = new HttpParams().set('date', date).set('startTime', startTime);
    for (const id of serviceIds) {
      params = params.append('serviceIds', id);
    }
    return this.http.get<SlotCheckResultDto>(`${this.base}/check-slot`, { params });
  }

  create(body: CreateLaserAppointmentRequest): Observable<LaserAppointmentDto> {
    return this.http.post<LaserAppointmentDto>(this.base, body);
  }

  update(
    id: string,
    body: {
      appointmentDate: string;
      startTime: string;
      laserServiceIds: string[];
      notes?: string | null;
      serviceDurationOverrides?: {
        serviceId: string;
        durationMinutes: number;
        pulsesConsumed?: number | null;
      }[] | null;
    }
  ): Observable<LaserAppointmentDto> {
    return this.http.put<LaserAppointmentDto>(`${this.base}/${id}`, body);
  }

  getSession(id: string): Observable<SessionDetailDto> {
    return this.http.get<SessionDetailDto>(`${this.base}/${id}/session`);
  }

  recordSession(id: string, body: RecordSessionRequest): Observable<SessionDetailDto> {
    return this.http.put<SessionDetailDto>(`${this.base}/${id}/session`, body);
  }

  bookWithCustomer(body: CreateBookingWithCustomerRequest): Observable<LaserAppointmentDto> {
    return this.http.post<LaserAppointmentDto>(`${this.base}/book-with-customer`, body);
  }

  updateStatus(id: string, status: LaserAppointmentStatus): Observable<LaserAppointmentDto> {
    const body: UpdateLaserAppointmentStatusRequest = { status };
    return this.http.put<LaserAppointmentDto>(`${this.base}/${id}/status`, body);
  }

  cancel(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
