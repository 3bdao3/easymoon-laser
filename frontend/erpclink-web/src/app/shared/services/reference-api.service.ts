import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ClinicDetail,
  DoctorDetail,
  PagedClinicsResult,
  PagedDoctorsResult,
  PagedPatientsResult,
  SearchClinicsParams,
  SearchDoctorsParams,
  SearchPatientsParams
} from '../models/reference.models';
import { toHttpParams } from '../utils/http-params.util';

@Injectable({ providedIn: 'root' })
export class ReferenceApiService {
  private readonly http = inject(HttpClient);

  searchDoctors(params: SearchDoctorsParams): Observable<PagedDoctorsResult> {
    return this.http.get<PagedDoctorsResult>('/api/v1/doctors/search', {
      params: toHttpParams(params)
    });
  }

  getDoctor(id: string): Observable<DoctorDetail> {
    return this.http.get<DoctorDetail>(`/api/v1/doctors/${id}`);
  }

  searchPatients(params: SearchPatientsParams): Observable<PagedPatientsResult> {
    return this.http.get<PagedPatientsResult>('/api/v1/patients/search', {
      params: toHttpParams(params)
    });
  }

  searchClinics(params: SearchClinicsParams): Observable<PagedClinicsResult> {
    return this.http.get<PagedClinicsResult>('/api/v1/clinics/search', {
      params: toHttpParams(params)
    });
  }

  getClinic(id: string): Observable<ClinicDetail> {
    return this.http.get<ClinicDetail>(`/api/v1/clinics/${id}`);
  }
}
