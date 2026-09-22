import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SimplePagedResult } from '../../../shared/models/api.models';
import {
  PatientDocumentDto,
  PatientDocumentSummaryDto,
  SearchPatientDocumentsParams,
  UpdatePatientDocumentMetadataRequest,
  UploadPatientDocumentResult
} from '../models/patient-document.models';

@Injectable({ providedIn: 'root' })
export class PatientDocumentsApi {
  private readonly http = inject(HttpClient);

  private base(patientId: string): string {
    return `${environment.apiBaseUrl}/api/v1/patients/${patientId}/documents`;
  }

  search(
    patientId: string,
    params: SearchPatientDocumentsParams
  ): Observable<SimplePagedResult<PatientDocumentDto>> {
    let httpParams = new HttpParams();
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== null && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    }
    return this.http.get<SimplePagedResult<PatientDocumentDto>>(this.base(patientId), {
      params: httpParams
    });
  }

  summary(patientId: string): Observable<PatientDocumentSummaryDto> {
    return this.http.get<PatientDocumentSummaryDto>(`${this.base(patientId)}/summary`);
  }

  getById(patientId: string, documentId: string): Observable<PatientDocumentDto> {
    return this.http.get<PatientDocumentDto>(`${this.base(patientId)}/${documentId}`);
  }

  download(patientId: string, documentId: string): Observable<Blob> {
    return this.http.get(`${this.base(patientId)}/${documentId}/download`, {
      responseType: 'blob'
    });
  }

  upload(
    patientId: string,
    form: FormData
  ): Observable<UploadPatientDocumentResult> {
    return this.http.post<UploadPatientDocumentResult>(this.base(patientId), form);
  }

  updateMetadata(
    patientId: string,
    documentId: string,
    body: UpdatePatientDocumentMetadataRequest
  ): Observable<PatientDocumentDto> {
    return this.http.put<PatientDocumentDto>(`${this.base(patientId)}/${documentId}`, body);
  }

  deactivate(patientId: string, documentId: string): Observable<void> {
    return this.http.post<void>(`${this.base(patientId)}/${documentId}/deactivate`, null);
  }
}
