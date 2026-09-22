import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateSpecialtyRequest,
  SpecialtyDto,
  UpdateSpecialtyRequest
} from '../models/specialty.models';

@Injectable({ providedIn: 'root' })
export class SpecialtiesApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/specialties`;

  list(isActive?: boolean): Observable<SpecialtyDto[]> {
    let params = new HttpParams();
    if (isActive !== undefined) {
      params = params.set('isActive', String(isActive));
    }
    return this.http.get<SpecialtyDto[]>(this.base, { params });
  }

  create(body: CreateSpecialtyRequest): Observable<SpecialtyDto> {
    return this.http.post<SpecialtyDto>(this.base, body);
  }

  update(id: string, body: UpdateSpecialtyRequest): Observable<SpecialtyDto> {
    return this.http.put<SpecialtyDto>(`${this.base}/${id}`, body);
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, null);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, null);
  }
}
