export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/** Backend paged payloads (Patients, Doctors, Clinics) without computed paging fields. */
export interface SimplePagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function enrichPagedResult<T>(page: SimplePagedResult<T>): PagedResult<T> {
  const totalPages = Math.max(1, Math.ceil(page.totalCount / page.pageSize));
  return {
    ...page,
    totalPages,
    hasPreviousPage: page.page > 1,
    hasNextPage: page.page < totalPages
  };
}
