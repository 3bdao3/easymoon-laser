import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toHttpParams } from '../../../shared/utils/http-params.util';
import {
  CreateDoctorScheduleRequest,
  DoctorAvailability,
  DoctorSchedule,
  UpdateDoctorScheduleRequest
} from '../models/scheduling.models';

@Injectable({ providedIn: 'root' })
export class SchedulingApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/scheduling';

  createSchedule(body: CreateDoctorScheduleRequest): Observable<DoctorSchedule> {
    return this.http.post<DoctorSchedule>(`${this.base}/schedules`, body);
  }

  getSchedule(id: string): Observable<DoctorSchedule> {
    return this.http.get<DoctorSchedule>(`${this.base}/schedules/${id}`);
  }

  listByDoctor(doctorId: string, isActive?: boolean): Observable<DoctorSchedule[]> {
    return this.http.get<DoctorSchedule[]>(`${this.base}/doctors/${doctorId}/schedules`, {
      params: toHttpParams({ isActive })
    });
  }

  updateSchedule(id: string, body: UpdateDoctorScheduleRequest): Observable<DoctorSchedule> {
    return this.http.put<DoctorSchedule>(`${this.base}/schedules/${id}`, body);
  }

  activateSchedule(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/schedules/${id}/activate`, null);
  }

  deactivateSchedule(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/schedules/${id}/deactivate`, null);
  }

  getAvailability(doctorId: string, date: string, clinicId?: string): Observable<DoctorAvailability> {
    return this.http.get<DoctorAvailability>(`${this.base}/doctors/${doctorId}/availability`, {
      params: toHttpParams({ date, clinicId })
    });
  }
}
