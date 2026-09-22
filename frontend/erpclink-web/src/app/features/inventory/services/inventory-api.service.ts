import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/inventory';

  searchWarehouses(query?: string): Observable<PagedWarehouses> {
    let params = new HttpParams();
    if (query) params = params.set('query', query);
    return this.http.get<PagedWarehouses>(`${this.base}/warehouses`, { params });
  }

  createWarehouse(name: string, description?: string): Observable<WarehouseDto> {
    return this.http.post<WarehouseDto>(`${this.base}/warehouses`, { name, description });
  }

  searchItems(query?: string): Observable<PagedItems> {
    let params = new HttpParams();
    if (query) params = params.set('query', query);
    return this.http.get<PagedItems>(`${this.base}/items`, { params });
  }

  createItem(body: CreateItemBody): Observable<ItemDto> {
    return this.http.post<ItemDto>(`${this.base}/items`, body);
  }

  searchBalances(warehouseId?: string, inventoryItemId?: string): Observable<PagedBalances> {
    let params = new HttpParams();
    if (warehouseId) params = params.set('warehouseId', warehouseId);
    if (inventoryItemId) params = params.set('inventoryItemId', inventoryItemId);
    return this.http.get<PagedBalances>(`${this.base}/stock/balances`, { params });
  }

  getReceivablePo(purchaseOrderId: string): Observable<ReceivablePo> {
    return this.http.get<ReceivablePo>(`${this.base}/goods-receipts/receivable-purchase-orders/${purchaseOrderId}`);
  }

  createGoodsReceipt(body: CreateGrBody): Observable<GoodsReceiptDto> {
    return this.http.post<GoodsReceiptDto>(`${this.base}/goods-receipts/from-purchase-order`, body);
  }

  getValuation(warehouseId?: string, inventoryItemId?: string): Observable<ValuationResult> {
    let params = new HttpParams();
    if (warehouseId) params = params.set('warehouseId', warehouseId);
    if (inventoryItemId) params = params.set('inventoryItemId', inventoryItemId);
    return this.http.get<ValuationResult>(`${this.base}/valuation`, { params });
  }

  getCostLayers(inventoryItemId?: string): Observable<{ items: CostLayerDto[] }> {
    let params = new HttpParams();
    if (inventoryItemId) params = params.set('inventoryItemId', inventoryItemId);
    return this.http.get<{ items: CostLayerDto[] }>(`${this.base}/valuation/cost-layers`, { params });
  }

  getIssueCosts(): Observable<{ items: IssueCostLineDto[]; totalIssueCost: number }> {
    return this.http.get<{ items: IssueCostLineDto[]; totalIssueCost: number }>(`${this.base}/valuation/issue-costs`);
  }

  getCostHistory(inventoryItemId: string): Observable<{ items: CostHistoryLineDto[] }> {
    let params = new HttpParams().set('inventoryItemId', inventoryItemId);
    return this.http.get<{ items: CostHistoryLineDto[] }>(`${this.base}/valuation/cost-history`, { params });
  }
}

export interface CostHistoryLineDto {
  id: string;
  transactionDate: string;
  eventType: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  sourceType: string;
  sourceId: string;
}

export interface ValuationResult {
  totalQuantity: number;
  totalValue: number;
  valuationMethod: string;
  items: {
    warehouseId: string;
    inventoryItemId: string;
    itemCode?: string | null;
    itemName?: string | null;
    quantity: number;
    inventoryValue: number;
    averageUnitCost?: number | null;
  }[];
}

export interface CostLayerDto {
  id: string;
  receiptDate: string;
  remainingQuantity: number;
  unitCost: number;
  remainingValue: number;
  status: string;
}

export interface IssueCostLineDto {
  id: string;
  transactionDate: string;
  eventType: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
}

export interface WarehouseDto {
  id: string;
  warehouseCode: string;
  name: string;
  isActive: boolean;
}

export interface PagedWarehouses {
  items: WarehouseDto[];
  totalCount: number;
}

export interface ItemDto {
  id: string;
  itemCode: string;
  name: string;
  trackExpiry: boolean;
  unitOfMeasure: string;
}

export interface CreateItemBody {
  name: string;
  unitOfMeasure: string;
  trackExpiry: boolean;
  minStockQuantity: number;
}

export interface PagedItems {
  items: ItemDto[];
  totalCount: number;
}

export interface PagedBalances {
  items: { warehouseId: string; inventoryItemId: string; quantity: number }[];
}

export interface ReceivablePo {
  id: string;
  purchaseOrderNumber: string;
  rowVersion: string;
  lines: {
    lineId: string;
    descriptionSnapshot: string;
    quantityOrdered: number;
    quantityReceived: number;
    unitCost: number;
  }[];
}

export interface CreateGrBody {
  purchaseOrderId: string;
  warehouseId: string;
  receiptDate: string;
  purchaseOrderRowVersion?: string;
  lines: { purchaseOrderLineId: string; inventoryItemId: string; quantity: number; batchNumber?: string; expiryDate?: string }[];
}

export interface GoodsReceiptDto {
  id: string;
  receiptNumber: string;
}
