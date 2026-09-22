export interface HealthcareServiceDto {
  id: string;
  organizationId: string;
  serviceCode: string;
  name: string;
  description: string | null;
  categoryId: string | null;
  defaultPrice: number;
  currencyCode: string;
  durationMinutes: number | null;
  isActive: boolean;
  currentPriceId: string | null;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
  rowVersion: string;
}

export interface HealthcareServiceListItemDto {
  id: string;
  serviceCode: string;
  name: string;
  categoryId: string | null;
  defaultPrice: number;
  currencyCode: string;
  isActive: boolean;
}

export interface PagedHealthcareServicesResult {
  items: HealthcareServiceListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateHealthcareServiceRequest {
  serviceCode: string;
  name: string;
  description?: string | null;
  categoryId?: string | null;
  defaultPrice: number;
  currencyCode: string;
  durationMinutes?: number | null;
}

export interface UpdateHealthcareServiceRequest {
  name: string;
  description?: string | null;
  categoryId?: string | null;
  defaultPrice?: number | null;
  currencyCode?: string | null;
  durationMinutes?: number | null;
  rowVersion?: string | null;
}

export interface SearchHealthcareServicesParams {
  query?: string;
  serviceCode?: string;
  name?: string;
  categoryId?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

export interface ServiceCategoryDto {
  id: string;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface PagedServiceCategoriesResult {
  items: ServiceCategoryDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}
