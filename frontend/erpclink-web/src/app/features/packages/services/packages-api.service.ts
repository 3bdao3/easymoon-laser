import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateHealthcarePackageRequest,
  HealthcarePackageDto,
  PackageItemInput,
  PagedHealthcarePackagesResult,
  SearchHealthcarePackagesParams,
  UpdateHealthcarePackageRequest,
  UpdatePackageItemRequest
} from '../models/package.models';

@Injectable({ providedIn: 'root' })
export class PackagesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/packages`;

  create(body: CreateHealthcarePackageRequest): Observable<HealthcarePackageDto> {
    return this.http.post<HealthcarePackageDto>(this.base, body);
  }

  getById(id: string): Observable<HealthcarePackageDto> {
    return this.http.get<HealthcarePackageDto>(`${this.base}/${id}`);
  }

  search(params: SearchHealthcarePackagesParams): Observable<PagedHealthcarePackagesResult> {
    return this.http.get<PagedHealthcarePackagesResult>(`${this.base}/search`, { params: toParams(params) });
  }

  update(id: string, body: UpdateHealthcarePackageRequest): Observable<HealthcarePackageDto> {
    return this.http.put<HealthcarePackageDto>(`${this.base}/${id}`, body);
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, null);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, null);
  }

  addItem(id: string, body: PackageItemInput): Observable<HealthcarePackageDto> {
    return this.http.post<HealthcarePackageDto>(`${this.base}/${id}/items`, body);
  }

  updateItem(id: string, itemId: string, body: UpdatePackageItemRequest): Observable<HealthcarePackageDto> {
    return this.http.put<HealthcarePackageDto>(`${this.base}/${id}/items/${itemId}`, body);
  }

  removeItem(id: string, itemId: string, rowVersion: string): Observable<HealthcarePackageDto> {
    const params = new HttpParams().set('rowVersion', rowVersion);
    return this.http.delete<HealthcarePackageDto>(`${this.base}/${id}/items/${itemId}`, { params });
  }
}

function toParams(input: SearchHealthcarePackagesParams): HttpParams {
  let params = new HttpParams();
  const entries: [string, string | number | boolean | undefined | null][] = [
    ['query', input.query],
    ['packageCode', input.packageCode],
    ['isActive', input.isActive],
    ['page', input.page],
    ['pageSize', input.pageSize]
  ];
  for (const [key, value] of entries) {
    if (value !== undefined && value !== null && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}
