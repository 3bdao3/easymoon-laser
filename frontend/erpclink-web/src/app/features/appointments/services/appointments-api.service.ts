import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toHttpParams } from '../../../shared/utils/http-params.util';
import {
  Appointment,
  BookAppointmentRequest,
  CancelAppointmentRequest,
  PagedAppointmentsResult,
  RescheduleAppointmentRequest,
  SearchAppointmentsParams
} from '../models/appointment.models';

@Injectable({ providedIn: 'root' })
export class AppointmentsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/appointments';

  book(body: BookAppointmentRequest): Observable<Appointment> {
    return this.http.post<Appointment>(this.base, body);
  }

  getById(id: string): Observable<Appointment> {
    return this.http.get<Appointment>(`${this.base}/${id}`);
  }

  search(params: SearchAppointmentsParams): Observable<PagedAppointmentsResult> {
    return this.http.get<PagedAppointmentsResult>(`${this.base}/search`, {
      params: toHttpParams(params)
    });
  }

  confirm(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/confirm`, null);
  }

  cancel(id: string, body: CancelAppointmentRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancel`, body);
  }

  reschedule(id: string, body: RescheduleAppointmentRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/reschedule`, body);
  }

  markNoShow(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/no-show`, null);
  }
}
