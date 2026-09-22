export interface PurchaseOrderLineDto {
  id: string;
  catalogItemId?: string | null;
  descriptionSnapshot: string;
  quantity: number;
  unitCost: number;
  lineDiscountAmount: number;
  lineSubtotal: number;
  lineTotal: number;
  sortOrder: number;
}

export interface PurchaseOrderDto {
  id: string;
  organizationId: string;
  branchId: string;
  purchaseOrderNumber: string;
  supplierId: string;
  orderDate: string;
  status: string;
  currencyCode: string;
  subTotal: number;
  discountAmount: number;
  taxAmount: number;
  totalAmount: number;
  notes?: string | null;
  submittedAtUtc?: string | null;
  approvedAtUtc?: string | null;
  cancelledAtUtc?: string | null;
  cancellationReason?: string | null;
  rowVersion: string;
  lines: PurchaseOrderLineDto[];
}

export interface PagedPurchaseOrdersResult {
  items: PurchaseOrderDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PurchaseOrderLineInput {
  catalogItemId?: string | null;
  descriptionSnapshot: string;
  quantity: number;
  unitCost: number;
  lineDiscountAmount?: number | null;
  sortOrder?: number | null;
}

export interface CreateDraftPurchaseOrderRequest {
  supplierId: string;
  orderDate: string;
  currencyCode: string;
  notes?: string | null;
  discountAmount?: number | null;
  lines?: PurchaseOrderLineInput[] | null;
}
