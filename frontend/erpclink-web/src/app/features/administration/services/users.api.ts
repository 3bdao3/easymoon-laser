import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CreateUserRequest, UpdateUserRequest, UserDto } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/admin/users`;

  list(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(this.base);
  }

  getById(id: string): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.base}/${id}`);
  }

  create(body: CreateUserRequest): Observable<UserDto> {
    return this.http.post<UserDto>(this.base, body);
  }

  update(id: string, body: UpdateUserRequest): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.base}/${id}`, body);
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, null);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, null);
  }
}
