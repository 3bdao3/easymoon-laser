import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  ClinicDto,
  ClinicListItemDto,
  CreateClinicRequest,
  PagedClinicsResult,
  SearchClinicsParams,
  UpdateClinicRequest
} from '../models/clinic.models';

@Injectable({ providedIn: 'root' })
export class ClinicsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/clinics`;

  search(params: SearchClinicsParams): Observable<PagedClinicsResult> {
    let httpParams = new HttpParams();
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== null && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    }
    return this.http.get<PagedClinicsResult>(`${this.base}/search`, { params: httpParams });
  }

  getById(id: string): Observable<ClinicDto> {
    return this.http.get<ClinicDto>(`${this.base}/${id}`);
  }

  create(body: CreateClinicRequest): Observable<ClinicDto> {
    return this.http.post<ClinicDto>(this.base, body);
  }

  update(id: string, body: UpdateClinicRequest): Observable<ClinicDto> {
    return this.http.put<ClinicDto>(`${this.base}/${id}`, body);
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, null);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, null);
  }

  listActive(pageSize = 200): Observable<ClinicListItemDto[]> {
    return this.http
      .get<PagedClinicsResult>(`${this.base}/search`, {
        params: new HttpParams().set('isActive', 'true').set('pageSize', String(pageSize))
      })
      .pipe(map((page) => page.items));
  }
}
