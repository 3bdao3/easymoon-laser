import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { LaserDashboardDto } from '../models/laser-clinic.models';

@Injectable({ providedIn: 'root' })
export class LaserDashboardApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/laser-dashboard`;

  get(date?: string): Observable<LaserDashboardDto> {
    let params = new HttpParams();
    if (date) {
      params = params.set('date', date);
    }
    return this.http.get<LaserDashboardDto>(this.base, { params });
  }
}
