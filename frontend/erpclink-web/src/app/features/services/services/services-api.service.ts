import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateHealthcareServiceRequest,
  HealthcareServiceDto,
  PagedHealthcareServicesResult,
  PagedServiceCategoriesResult,
  SearchHealthcareServicesParams,
  ServiceCategoryDto,
  UpdateHealthcareServiceRequest
} from '../models/service.models';

@Injectable({ providedIn: 'root' })
export class ServicesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/services`;
  private readonly categoriesBase = `${environment.apiBaseUrl}/api/v1/service-categories`;

  create(body: CreateHealthcareServiceRequest): Observable<HealthcareServiceDto> {
    return this.http.post<HealthcareServiceDto>(this.base, body);
  }

  getById(id: string): Observable<HealthcareServiceDto> {
    return this.http.get<HealthcareServiceDto>(`${this.base}/${id}`);
  }

  search(params: SearchHealthcareServicesParams): Observable<PagedHealthcareServicesResult> {
    return this.http.get<PagedHealthcareServicesResult>(`${this.base}/search`, { params: toParams(params) });
  }

  update(id: string, body: UpdateHealthcareServiceRequest): Observable<HealthcareServiceDto> {
    return this.http.put<HealthcareServiceDto>(`${this.base}/${id}`, body);
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, null);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, null);
  }

  searchCategories(isActive = true): Observable<PagedServiceCategoriesResult> {
    const params = new HttpParams().set('isActive', String(isActive)).set('pageSize', '100');
    return this.http.get<PagedServiceCategoriesResult>(`${this.categoriesBase}/search`, { params });
  }

  createCategory(code: string, name: string): Observable<ServiceCategoryDto> {
    return this.http.post<ServiceCategoryDto>(this.categoriesBase, { code, name, description: null });
  }
}

function toParams(input: SearchHealthcareServicesParams): HttpParams {
  let params = new HttpParams();
  const entries: [string, string | number | boolean | undefined | null][] = [
    ['query', input.query],
    ['serviceCode', input.serviceCode],
    ['name', input.name],
    ['categoryId', input.categoryId],
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
