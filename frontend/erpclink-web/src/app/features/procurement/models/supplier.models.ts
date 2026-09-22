export interface SupplierDto {
  id: string;
  organizationId: string;
  supplierCode: string;
  name: string;
  contactName?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  notes?: string | null;
  isActive: boolean;
  createdAtUtc: string;
  createdBy?: string | null;
  updatedAtUtc?: string | null;
  updatedBy?: string | null;
  rowVersion: string;
}

export interface PagedSuppliersResult {
  items: SupplierDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateSupplierRequest {
  name: string;
  contactName?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  notes?: string | null;
}

export interface UpdateSupplierRequest extends CreateSupplierRequest {
  rowVersion?: string | null;
}
