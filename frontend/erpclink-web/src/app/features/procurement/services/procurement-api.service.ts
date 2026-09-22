import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  CreateDraftPurchaseOrderRequest,
  PagedPurchaseOrdersResult,
  PurchaseOrderDto
} from '../models/purchase-order.models';
import {
  CreateSupplierRequest,
  PagedSuppliersResult,
  SupplierDto,
  UpdateSupplierRequest
} from '../models/supplier.models';

@Injectable({ providedIn: 'root' })
export class ProcurementApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/procurement';

  searchSuppliers(params: { search?: string; isActive?: boolean; page?: number; pageSize?: number }) {
    let p = new HttpParams();
    if (params.search) p = p.set('search', params.search);
    if (params.isActive !== undefined) p = p.set('isActive', String(params.isActive));
    p = p.set('page', String(params.page ?? 1)).set('pageSize', String(params.pageSize ?? 20));
    return this.http.get<PagedSuppliersResult>(`${this.base}/suppliers/search`, { params: p });
  }

  getSupplier(id: string) {
    return this.http.get<SupplierDto>(`${this.base}/suppliers/${id}`);
  }

  createSupplier(body: CreateSupplierRequest) {
    return this.http.post<SupplierDto>(`${this.base}/suppliers`, body);
  }

  updateSupplier(id: string, body: UpdateSupplierRequest) {
    return this.http.put<SupplierDto>(`${this.base}/suppliers/${id}`, body);
  }

  activateSupplier(id: string, rowVersion?: string) {
    const params = rowVersion ? new HttpParams().set('rowVersion', rowVersion) : undefined;
    return this.http.post(`${this.base}/suppliers/${id}/activate`, null, { params });
  }

  deactivateSupplier(id: string, rowVersion?: string) {
    const params = rowVersion ? new HttpParams().set('rowVersion', rowVersion) : undefined;
    return this.http.post(`${this.base}/suppliers/${id}/deactivate`, null, { params });
  }

  searchPurchaseOrders(params: {
    supplierId?: string;
    purchaseOrderNumber?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }) {
    let p = new HttpParams();
    if (params.supplierId) p = p.set('supplierId', params.supplierId);
    if (params.purchaseOrderNumber) p = p.set('purchaseOrderNumber', params.purchaseOrderNumber);
    if (params.status) p = p.set('status', params.status);
    p = p.set('page', String(params.page ?? 1)).set('pageSize', String(params.pageSize ?? 20));
    return this.http.get<PagedPurchaseOrdersResult>(`${this.base}/purchase-orders/search`, { params: p });
  }

  getPurchaseOrder(id: string) {
    return this.http.get<PurchaseOrderDto>(`${this.base}/purchase-orders/${id}`);
  }

  createPurchaseOrderDraft(body: CreateDraftPurchaseOrderRequest) {
    return this.http.post<PurchaseOrderDto>(`${this.base}/purchase-orders`, body);
  }

  submitPurchaseOrder(id: string, rowVersion?: string) {
    const params = rowVersion ? new HttpParams().set('rowVersion', rowVersion) : undefined;
    return this.http.post<PurchaseOrderDto>(`${this.base}/purchase-orders/${id}/submit`, null, { params });
  }

  approvePurchaseOrder(id: string, rowVersion?: string) {
    const params = rowVersion ? new HttpParams().set('rowVersion', rowVersion) : undefined;
    return this.http.post<PurchaseOrderDto>(`${this.base}/purchase-orders/${id}/approve`, null, { params });
  }

  cancelPurchaseOrder(id: string, reason: string | null, rowVersion?: string) {
    return this.http.post<PurchaseOrderDto>(`${this.base}/purchase-orders/${id}/cancel`, {
      reason,
      rowVersion
    });
  }
}
