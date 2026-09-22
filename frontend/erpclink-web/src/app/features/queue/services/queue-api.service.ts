import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toHttpParams } from '../../../shared/utils/http-params.util';
import { CheckInRequest, QueueEntry, QueueListItem, TodayQueueParams } from '../models/queue.models';

@Injectable({ providedIn: 'root' })
export class QueueApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/queue';

  checkIn(body: CheckInRequest): Observable<QueueEntry> {
    return this.http.post<QueueEntry>(`${this.base}/check-in`, body);
  }

  getToday(params: TodayQueueParams): Observable<QueueListItem[]> {
    return this.http.get<QueueListItem[]>(`${this.base}/today`, {
      params: toHttpParams(params)
    });
  }

  call(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/call`, null);
  }

  startService(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/start-service`, null);
  }

  complete(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/complete`, null);
  }

  skip(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/skip`, null);
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancel`, null);
  }
}
