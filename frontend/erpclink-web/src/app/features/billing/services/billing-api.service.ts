import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  CreateDraftInvoiceRequest,
  InvoiceDto,
  PagedInvoicesResult,
  PatientOutstandingDto,
  RecordPaymentRequest,
  UpdateDraftInvoiceRequest
} from '../models/invoice.models';

@Injectable({ providedIn: 'root' })
export class BillingApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/billing';

  search(params: {
    patientId?: string;
    invoiceNumber?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }) {
    let p = new HttpParams();
    if (params.patientId) p = p.set('patientId', params.patientId);
    if (params.invoiceNumber) p = p.set('invoiceNumber', params.invoiceNumber);
    if (params.status) p = p.set('status', params.status);
    p = p.set('page', String(params.page ?? 1)).set('pageSize', String(params.pageSize ?? 20));
    return this.http.get<PagedInvoicesResult>(`${this.base}/invoices/search`, { params: p });
  }

  getById(id: string) {
    return this.http.get<InvoiceDto>(`${this.base}/invoices/${id}`);
  }

  createDraft(body: CreateDraftInvoiceRequest) {
    return this.http.post<InvoiceDto>(`${this.base}/invoices`, body);
  }

  updateDraft(id: string, body: UpdateDraftInvoiceRequest) {
    return this.http.put<InvoiceDto>(`${this.base}/invoices/${id}`, body);
  }

  issue(id: string) {
    return this.http.post<InvoiceDto>(`${this.base}/invoices/${id}/issue`, null);
  }

  void(id: string, reason?: string, rowVersion?: string) {
    return this.http.post<InvoiceDto>(`${this.base}/invoices/${id}/void`, { reason, rowVersion });
  }

  patientOutstanding(patientId: string) {
    return this.http.get<PatientOutstandingDto>(`${this.base}/invoices/patients/${patientId}/outstanding`);
  }

  patientInvoices(
    patientId: string,
    params: { page?: number; pageSize?: number; status?: string } = {}
  ) {
    let p = new HttpParams()
      .set('page', String(params.page ?? 1))
      .set('pageSize', String(params.pageSize ?? 20));
    if (params.status) {
      p = p.set('status', params.status);
    }
    return this.http.get<PagedInvoicesResult>(`${this.base}/invoices/patients/${patientId}`, {
      params: p
    });
  }

  recordPayment(body: RecordPaymentRequest) {
    return this.http.post(`${this.base}/payments`, body);
  }
}
