import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AssignDoctorToClinicRequest,
  DoctorClinicAssignmentDto,
  DoctorDto,
  DoctorListItemDto,
  PagedDoctorsResult,
  RegisterDoctorRequest,
  SearchDoctorsParams,
  UpdateDoctorRequest
} from '../models/doctor.models';
import { SimplePagedResult } from '../../../shared/models/api.models';

@Injectable({ providedIn: 'root' })
export class DoctorsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/doctors`;

  search(params: SearchDoctorsParams): Observable<PagedDoctorsResult> {
    let httpParams = new HttpParams();
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== null && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    }
    return this.http.get<SimplePagedResult<DoctorListItemDto>>(`${this.base}/search`, {
      params: httpParams
    });
  }

  getById(id: string): Observable<DoctorDto> {
    return this.http.get<DoctorDto>(`${this.base}/${id}`);
  }

  register(body: RegisterDoctorRequest): Observable<DoctorDto> {
    return this.http.post<DoctorDto>(this.base, body);
  }

  update(id: string, body: UpdateDoctorRequest): Observable<DoctorDto> {
    return this.http.put<DoctorDto>(`${this.base}/${id}`, body);
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, null);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, null);
  }

  getClinics(id: string): Observable<DoctorClinicAssignmentDto[]> {
    return this.http.get<DoctorClinicAssignmentDto[]>(`${this.base}/${id}/clinics`);
  }

  assignClinic(id: string, body: AssignDoctorToClinicRequest): Observable<DoctorClinicAssignmentDto> {
    return this.http.post<DoctorClinicAssignmentDto>(`${this.base}/${id}/clinics`, body);
  }

  deactivateClinicAssignment(doctorId: string, clinicId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${doctorId}/clinics/${clinicId}/deactivate`, null);
  }
}
