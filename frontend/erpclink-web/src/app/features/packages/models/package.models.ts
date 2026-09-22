export interface PackageItemDto {
  id: string;
  serviceId: string;
  serviceCode: string;
  serviceName: string;
  quantity: number;
  sortOrder: number;
}

export interface HealthcarePackageDto {
  id: string;
  organizationId: string;
  packageCode: string;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAtUtc: string;
  createdBy: string | null;
  updatedAtUtc: string | null;
  updatedBy: string | null;
  rowVersion: string;
  items: PackageItemDto[];
}

export interface HealthcarePackageListItemDto {
  id: string;
  packageCode: string;
  name: string;
  isActive: boolean;
  itemCount: number;
}

export interface PagedHealthcarePackagesResult {
  items: HealthcarePackageListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateHealthcarePackageRequest {
  packageCode: string;
  name: string;
  description?: string | null;
  items?: { serviceId: string; quantity: number; sortOrder?: number | null }[] | null;
}

export interface UpdateHealthcarePackageRequest {
  name: string;
  description?: string | null;
  rowVersion?: string | null;
}

export interface PackageItemInput {
  serviceId: string;
  quantity: number;
  sortOrder?: number | null;
}

export interface UpdatePackageItemRequest {
  quantity: number;
  sortOrder: number;
  rowVersion?: string | null;
}

export interface SearchHealthcarePackagesParams {
  query?: string;
  packageCode?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}
