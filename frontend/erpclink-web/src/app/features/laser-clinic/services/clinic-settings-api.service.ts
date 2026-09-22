import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  ClinicSettingsDto,
  UpdateClinicSettingsRequest
} from '../models/laser-clinic.models';

@Injectable({ providedIn: 'root' })
export class ClinicSettingsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/clinic-settings`;

  get(): Observable<ClinicSettingsDto> {
    return this.http.get<ClinicSettingsDto>(this.base);
  }

  update(body: UpdateClinicSettingsRequest): Observable<ClinicSettingsDto> {
    return this.http.put<ClinicSettingsDto>(this.base, body);
  }
}
