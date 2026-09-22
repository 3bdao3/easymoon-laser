import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateCustomerRequest,
  CustomerDto,
  CustomerHistoryDto,
  CustomerListItemDto,
  CustomerListSort,
  UpdateCustomerRequest
} from '../models/laser-clinic.models';

@Injectable({ providedIn: 'root' })
export class CustomersApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/customers`;

  search(q?: string, isActive?: boolean, sort: CustomerListSort = 'Name'): Observable<CustomerListItemDto[]> {
    let params = new HttpParams().set('sort', sort);
    if (q) {
      params = params.set('q', q);
    }
    if (isActive !== undefined) {
      params = params.set('isActive', String(isActive));
    }
    return this.http.get<CustomerListItemDto[]>(this.base, { params });
  }

  getById(id: string): Observable<CustomerDto> {
    return this.http.get<CustomerDto>(`${this.base}/${id}`);
  }

  create(body: CreateCustomerRequest): Observable<CustomerDto> {
    return this.http.post<CustomerDto>(this.base, body);
  }

  update(id: string, body: UpdateCustomerRequest): Observable<CustomerDto> {
    return this.http.put<CustomerDto>(`${this.base}/${id}`, body);
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, null);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, null);
  }

  getHistory(id: string): Observable<CustomerHistoryDto> {
    return this.http.get<CustomerHistoryDto>(`${this.base}/${id}/history`);
  }
}
