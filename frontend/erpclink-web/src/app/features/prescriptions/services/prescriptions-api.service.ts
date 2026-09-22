import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CancelPrescriptionRequest,
  CreatePrescriptionRequest,
  PagedPrescriptionsResult,
  PatientPrescriptionHistoryParams,
  PrescriptionDto,
  PrescriptionItemInput,
  SearchPrescriptionsParams,
  UpdatePrescriptionItemRequest,
  UpdatePrescriptionRequest
} from '../models/prescription.models';

@Injectable({ providedIn: 'root' })
export class PrescriptionsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/prescriptions`;

  create(body: CreatePrescriptionRequest): Observable<PrescriptionDto> {
    return this.http.post<PrescriptionDto>(this.base, body);
  }

  getById(id: string): Observable<PrescriptionDto> {
    return this.http.get<PrescriptionDto>(`${this.base}/${id}`);
  }

  search(params: SearchPrescriptionsParams): Observable<PagedPrescriptionsResult> {
    return this.http.get<PagedPrescriptionsResult>(`${this.base}/search`, {
      params: toParams(params as object)
    });
  }

  patientHistory(
    patientId: string,
    params: PatientPrescriptionHistoryParams
  ): Observable<PagedPrescriptionsResult> {
    return this.http.get<PagedPrescriptionsResult>(`${this.base}/patients/${patientId}/history`, {
      params: toParams(params as object)
    });
  }

  update(id: string, body: UpdatePrescriptionRequest): Observable<PrescriptionDto> {
    return this.http.put<PrescriptionDto>(`${this.base}/${id}`, body);
  }

  addItem(id: string, body: PrescriptionItemInput): Observable<PrescriptionDto> {
    return this.http.post<PrescriptionDto>(`${this.base}/${id}/items`, body);
  }

  updateItem(id: string, itemId: string, body: UpdatePrescriptionItemRequest): Observable<PrescriptionDto> {
    return this.http.put<PrescriptionDto>(`${this.base}/${id}/items/${itemId}`, body);
  }

  removeItem(id: string, itemId: string, rowVersion: string): Observable<PrescriptionDto> {
    const params = new HttpParams().set('rowVersion', rowVersion);
    return this.http.delete<PrescriptionDto>(`${this.base}/${id}/items/${itemId}`, { params });
  }

  issue(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/issue`, null);
  }

  cancel(id: string, body: CancelPrescriptionRequest): Observable<void> {
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
