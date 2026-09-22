import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SimplePagedResult } from '../../../shared/models/api.models';
import {
  AddAllergyRequest,
  AllergyDto,
  PatientDto,
  PatientListItemDto,
  RegisterPatientRequest,
  RegisterPatientResult,
  SearchPatientsParams,
  UpdateAllergyRequest,
  UpdatePatientRequest
} from '../models/patient.models';

@Injectable({ providedIn: 'root' })
export class PatientsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/patients`;

  search(params: SearchPatientsParams): Observable<SimplePagedResult<PatientListItemDto>> {
    let httpParams = new HttpParams();
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== null && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    }
    return this.http.get<SimplePagedResult<PatientListItemDto>>(`${this.base}/search`, {
      params: httpParams
    });
  }

  getById(id: string): Observable<PatientDto> {
    return this.http.get<PatientDto>(`${this.base}/${id}`);
  }

  register(body: RegisterPatientRequest): Observable<RegisterPatientResult> {
    return this.http.post<RegisterPatientResult>(this.base, body);
  }

  update(id: string, body: UpdatePatientRequest): Observable<PatientDto> {
    return this.http.put<PatientDto>(`${this.base}/${id}`, body);
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, null);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, null);
  }

  getAllergies(patientId: string): Observable<AllergyDto[]> {
    return this.http.get<AllergyDto[]>(`${this.base}/${patientId}/allergies`);
  }

  addAllergy(patientId: string, body: AddAllergyRequest): Observable<AllergyDto> {
    return this.http.post<AllergyDto>(`${this.base}/${patientId}/allergies`, body);
  }

  updateAllergy(
    patientId: string,
    allergyId: string,
    body: UpdateAllergyRequest
  ): Observable<AllergyDto> {
    return this.http.put<AllergyDto>(`${this.base}/${patientId}/allergies/${allergyId}`, body);
  }

  deactivateAllergy(patientId: string, allergyId: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/${patientId}/allergies/${allergyId}/deactivate`,
      null
    );
  }
}
