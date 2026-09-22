import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CancelMedicalVisitRequest,
  MedicalVisitDto,
  PagedMedicalVisitsResult,
  PatientVisitContextDto,
  PatientVisitHistoryParams,
  SearchMedicalVisitsParams,
  StartMedicalVisitRequest,
  UpdateClinicalNotesRequest
} from '../models/medical-visit.models';

@Injectable({ providedIn: 'root' })
export class MedicalVisitsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/medical-visits`;

  start(request: StartMedicalVisitRequest): Observable<MedicalVisitDto> {
    return this.http.post<MedicalVisitDto>(`${this.base}/start`, request);
  }

  getById(id: string): Observable<MedicalVisitDto> {
    return this.http.get<MedicalVisitDto>(`${this.base}/${id}`);
  }

  getByNumber(visitNumber: string): Observable<MedicalVisitDto> {
    return this.http.get<MedicalVisitDto>(`${this.base}/by-number/${encodeURIComponent(visitNumber)}`);
  }

  search(params: SearchMedicalVisitsParams): Observable<PagedMedicalVisitsResult> {
    return this.http.get<PagedMedicalVisitsResult>(`${this.base}/search`, {
      params: toParams(params as object)
    });
  }

  patientHistory(patientId: string, params: PatientVisitHistoryParams): Observable<PagedMedicalVisitsResult> {
    return this.http.get<PagedMedicalVisitsResult>(`${this.base}/patients/${patientId}/history`, {
      params: toParams(params as object)
    });
  }

  patientVisitContext(patientId: string): Observable<PatientVisitContextDto> {
    return this.http.get<PatientVisitContextDto>(`${this.base}/patients/${patientId}/context`);
  }

  updateClinicalNotes(id: string, body: UpdateClinicalNotesRequest): Observable<MedicalVisitDto> {
    return this.http.put<MedicalVisitDto>(`${this.base}/${id}/clinical-notes`, body);
  }

  complete(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/complete`, null);
  }

  cancel(id: string, body: CancelMedicalVisitRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancel`, body);
  }
}

function toParams(input: object): HttpParams {
  const record = input as Record<string, string | number | boolean | undefined | null>;
  let params = new HttpParams();
  for (const [key, value] of Object.entries(record)) {
    if (value === undefined || value === null || value === '') {
      continue;
    }
    params = params.set(key, String(value));
  }
  return params;
}
