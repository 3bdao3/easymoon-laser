import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateLaserOfferRequest,
  LaserOfferDto,
  UpdateLaserOfferRequest
} from '../models/laser-clinic.models';

@Injectable({ providedIn: 'root' })
export class LaserOffersApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/laser-offers`;

  list(activeOnly = false): Observable<LaserOfferDto[]> {
    const params = new HttpParams().set('activeOnly', String(activeOnly));
    return this.http.get<LaserOfferDto[]>(this.base, { params });
  }

  getById(id: string): Observable<LaserOfferDto> {
    return this.http.get<LaserOfferDto>(`${this.base}/${id}`);
  }

  create(body: CreateLaserOfferRequest): Observable<LaserOfferDto> {
    return this.http.post<LaserOfferDto>(this.base, body);
  }

  update(id: string, body: UpdateLaserOfferRequest): Observable<LaserOfferDto> {
    return this.http.put<LaserOfferDto>(`${this.base}/${id}`, body);
  }
}
