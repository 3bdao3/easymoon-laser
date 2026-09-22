import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateMedicationRequest,
  MedicationDto,
  PagedMedicationsResult,
  SearchMedicationsParams,
  UpdateMedicationRequest
} from '../models/medication.models';

@Injectable({ providedIn: 'root' })
export class MedicationsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/medications`;

  create(body: CreateMedicationRequest): Observable<MedicationDto> {
    return this.http.post<MedicationDto>(this.base, body);
  }

  getById(id: string): Observable<MedicationDto> {
    return this.http.get<MedicationDto>(`${this.base}/${id}`);
  }

  search(params: SearchMedicationsParams): Observable<PagedMedicationsResult> {
    return this.http.get<PagedMedicationsResult>(`${this.base}/search`, {
      params: toParams(params as object)
    });
  }

  update(id: string, body: UpdateMedicationRequest): Observable<MedicationDto> {
    return this.http.put<MedicationDto>(`${this.base}/${id}`, body);
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, null);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, null);
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
