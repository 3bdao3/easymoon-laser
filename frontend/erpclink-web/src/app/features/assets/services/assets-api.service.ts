import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class AssetsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/assets';

  searchCategories(query?: string): Observable<PagedResult<AssetCategoryDto>> {
    let params = new HttpParams();
    if (query) params = params.set('query', query);
    return this.http.get<PagedResult<AssetCategoryDto>>(`${this.base}/categories`, { params });
  }

  createCategory(name: string, description?: string | null): Observable<AssetCategoryDto> {
    return this.http.post<AssetCategoryDto>(`${this.base}/categories`, { name, description });
  }

  searchLocations(query?: string): Observable<PagedResult<AssetLocationDto>> {
    let params = new HttpParams();
    if (query) params = params.set('query', query);
    return this.http.get<PagedResult<AssetLocationDto>>(`${this.base}/locations`, { params });
  }

  createLocation(name: string, description?: string | null): Observable<AssetLocationDto> {
    return this.http.post<AssetLocationDto>(`${this.base}/locations`, { name, description });
  }

  searchAssets(status?: string): Observable<PagedResult<AssetDto>> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<AssetDto>>(this.base, { params });
  }

  getAsset(id: string): Observable<AssetDto> {
    return this.http.get<AssetDto>(`${this.base}/${id}`);
  }

  createAsset(body: CreateAssetBody): Observable<AssetDto> {
    return this.http.post<AssetDto>(this.base, body);
  }

  changeLocation(id: string, assetLocationId: string, rowVersion: string): Observable<AssetDto> {
    return this.http.post<AssetDto>(`${this.base}/${id}/change-location`, { assetLocationId, rowVersion });
  }

  startMaintenance(id: string, notes: string | null, rowVersion: string): Observable<AssetDto> {
    return this.http.post<AssetDto>(`${this.base}/${id}/start-maintenance`, { notes, rowVersion });
  }

  completeMaintenance(id: string, rowVersion: string): Observable<AssetDto> {
    return this.http.post<AssetDto>(`${this.base}/${id}/complete-maintenance`, { rowVersion });
  }

  retire(id: string, reason: string, rowVersion: string): Observable<AssetDto> {
    return this.http.post<AssetDto>(`${this.base}/${id}/retire`, { reason, rowVersion });
  }

  getHistory(id: string): Observable<AssetHistoryEntryDto[]> {
    return this.http.get<AssetHistoryEntryDto[]>(`${this.base}/${id}/history`);
  }

  searchAccounting(): Observable<PagedResult<AssetAccountingListItem>> {
    return this.http.get<PagedResult<AssetAccountingListItem>>(`${this.base}/accounting`);
  }

  getFinancialProfile(assetId: string): Observable<AssetFinancialProfileDto> {
    return this.http.get<AssetFinancialProfileDto>(`${this.base}/accounting/${assetId}`);
  }

  capitalize(body: CapitalizeBody): Observable<AssetFinancialProfileDto> {
    return this.http.post<AssetFinancialProfileDto>(`${this.base}/accounting/capitalize`, body);
  }

  getSchedule(assetId: string): Observable<PagedResult<DepreciationScheduleLineDto>> {
    const params = new HttpParams().set('assetId', assetId);
    return this.http.get<PagedResult<DepreciationScheduleLineDto>>(`${this.base}/depreciation/schedule`, { params });
  }

  getDepreciation(assetId?: string): Observable<{ items: DepreciationTransactionDto[]; totalDepreciation: number }> {
    let params = new HttpParams();
    if (assetId) params = params.set('assetId', assetId);
    return this.http.get<{ items: DepreciationTransactionDto[]; totalDepreciation: number }>(
      `${this.base}/depreciation`,
      { params }
    );
  }

  postDepreciation(assetId: string): Observable<DepreciationTransactionDto> {
    return this.http.post<DepreciationTransactionDto>(`${this.base}/depreciation/post`, { assetId });
  }

  getFinancialHistory(assetId: string): Observable<PagedResult<AssetFinancialHistoryLineDto>> {
    return this.http.get<PagedResult<AssetFinancialHistoryLineDto>>(`${this.base}/accounting/${assetId}/history`);
  }

  getValuation(): Observable<{
    totalCapitalizedCost: number;
    totalAccumulatedDepreciation: number;
    totalNetBookValue: number;
    items: { assetNumber: string; assetName: string; netBookValue: number }[];
  }> {
    return this.http.get<{
      totalCapitalizedCost: number;
      totalAccumulatedDepreciation: number;
      totalNetBookValue: number;
      items: { assetNumber: string; assetName: string; netBookValue: number }[];
    }>(`${this.base}/valuation`);
  }

  getDisposals(): Observable<PagedResult<AssetDisposalDto>> {
    return this.http.get<PagedResult<AssetDisposalDto>>(`${this.base}/disposals`);
  }
}

export interface AssetAccountingListItem {
  assetId: string;
  assetNumber: string;
  assetName: string;
  financialStatus: string;
  capitalizedCost?: number | null;
  accumulatedDepreciation?: number | null;
  netBookValue?: number | null;
}

export interface AssetFinancialProfileDto {
  id: string;
  assetId: string;
  status: string;
  capitalizedCost: number;
  residualValue: number;
  accumulatedDepreciation: number;
  netBookValue: number;
  usefulLifeMonths: number;
  depreciationMethod: string;
  rowVersion: string;
}

export interface CapitalizeBody {
  assetId: string;
  acquisitionCost?: number | null;
  capitalizedCost: number;
  residualValue: number;
  capitalizationDate: string;
  depreciationStartDate: string;
  usefulLifeMonths: number;
}

export interface DepreciationScheduleLineDto {
  periodKey: string;
  plannedAmount: number;
  status: string;
}

export interface DepreciationTransactionDto {
  id: string;
  periodKey: string;
  depreciationAmount: number;
  closingNetBookValue: number;
}

export interface AssetFinancialHistoryLineDto {
  eventType: string;
  transactionDate: string;
  amount: number;
  description?: string | null;
}

export interface AssetDisposalDto {
  assetNumber: string;
  assetName: string;
  disposedDate: string;
  netBookValueAtDisposal: number;
  proceeds?: number | null;
  gainLoss?: number | null;
}

export interface AssetCategoryDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
}

export interface AssetLocationDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
}

export interface AssetDto {
  id: string;
  assetNumber: string;
  name: string;
  status: string;
  assetCategoryId: string;
  assetLocationId: string;
  purchaseDate?: string | null;
  warrantyStartDate?: string | null;
  warrantyEndDate?: string | null;
  maintenanceNotes?: string | null;
  rowVersion: string;
}

export interface AssetHistoryEntryDto {
  id: string;
  eventType: string;
  summary: string;
  oldValue?: string | null;
  newValue?: string | null;
  notes?: string | null;
  occurredAtUtc: string;
}

export interface CreateAssetBody {
  name: string;
  description?: string | null;
  assetCategoryId: string;
  assetLocationId: string;
  serialNumber?: string | null;
  purchaseDate?: string | null;
  acquisitionCost?: number | null;
  acquisitionReference?: string | null;
  warrantyStartDate?: string | null;
  warrantyEndDate?: string | null;
  warrantyNotes?: string | null;
}
