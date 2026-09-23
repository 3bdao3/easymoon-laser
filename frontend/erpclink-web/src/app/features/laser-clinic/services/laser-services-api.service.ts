import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateLaserServiceRequest,
  LaserServiceDto,
  UpdateLaserServiceRequest
} from '../models/laser-clinic.models';

@Injectable({ providedIn: 'root' })
export class LaserServicesApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/laser-services`;

  list(activeOnly = true): Observable<LaserServiceDto[]> {
    const params = new HttpParams().set('activeOnly', String(activeOnly));
    return this.http.get<LaserServiceDto[]>(this.base, { params });
  }

  getById(id: string): Observable<LaserServiceDto> {
    return this.http.get<LaserServiceDto>(`${this.base}/${id}`);
  }

  create(body: CreateLaserServiceRequest): Observable<LaserServiceDto> {
    return this.http.post<LaserServiceDto>(this.base, body);
  }

  update(id: string, body: UpdateLaserServiceRequest): Observable<LaserServiceDto> {
    return this.http.put<LaserServiceDto>(`${this.base}/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
