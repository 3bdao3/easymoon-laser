import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ReportPage<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  sourceModule: string;
  dateFilterSemantics?: string;
}

export interface MetricValue {
  metric: string;
  available: boolean;
  value: number | null;
  currencyCode: string | null;
  reason: string | null;
  source: string;
}

export interface ManagementSummaryResult {
  fromDate: string | null;
  toDate: string | null;
  metrics: MetricValue[];
  notes: string;
}

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/reports`;

  private params(q: Record<string, string | number | boolean | null | undefined>): HttpParams {
    let p = new HttpParams();
    for (const [k, v] of Object.entries(q)) {
      if (v !== null && v !== undefined && v !== '') {
        p = p.set(k, String(v));
      }
    }
    return p;
  }

  getGl(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/finance/general-ledger`, {
      params: this.params(q)
    });
  }

  getTrialBalance(fromDate: string, toDate: string, branchId?: string): Observable<Record<string, unknown>> {
    return this.http.get<Record<string, unknown>>(`${this.base}/finance/trial-balance`, {
      params: this.params({ fromDate, toDate, branchId })
    });
  }

  getJournals(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/finance/journals`, {
      params: this.params(q)
    });
  }

  getArStatement(partyId: string, q: Record<string, string | number | undefined>): Observable<Record<string, unknown>> {
    return this.http.get<Record<string, unknown>>(`${this.base}/ar/statement`, {
      params: this.params({ partyId, ...q })
    });
  }

  getArAging(q: Record<string, string | undefined>): Observable<Record<string, unknown>> {
    return this.http.get<Record<string, unknown>>(`${this.base}/ar/aging`, { params: this.params(q) });
  }

  getApStatement(partyId: string, q: Record<string, string | number | undefined>): Observable<Record<string, unknown>> {
    return this.http.get<Record<string, unknown>>(`${this.base}/ap/statement`, {
      params: this.params({ partyId, ...q })
    });
  }

  getApAging(q: Record<string, string | undefined>): Observable<Record<string, unknown>> {
    return this.http.get<Record<string, unknown>>(`${this.base}/ap/aging`, { params: this.params(q) });
  }

  getBillingInvoices(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/billing/invoices`, {
      params: this.params(q)
    });
  }

  getBillingPayments(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/billing/payments`, {
      params: this.params(q)
    });
  }

  getBillingOutstanding(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/billing/outstanding`, {
      params: this.params(q)
    });
  }

  getStock(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/inventory/stock`, {
      params: this.params(q)
    });
  }

  getMovements(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/inventory/movements`, {
      params: this.params(q)
    });
  }

  getInventoryValuation(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/inventory/valuation`, {
      params: this.params(q)
    });
  }

  getInventoryCogs(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/inventory/cogs`, {
      params: this.params(q)
    });
  }

  getAssetRegister(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/assets/register`, {
      params: this.params(q)
    });
  }

  getAssetDepreciation(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/assets/depreciation`, {
      params: this.params(q)
    });
  }

  getAssetValuation(page = 1, pageSize = 50): Observable<Record<string, unknown>> {
    return this.http.get<Record<string, unknown>>(`${this.base}/assets/valuation`, {
      params: this.params({ page, pageSize })
    });
  }

  getPurchaseOrders(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/procurement/purchase-orders`, {
      params: this.params(q)
    });
  }

  getReceiving(q: Record<string, string | number | undefined>): Observable<ReportPage<Record<string, unknown>>> {
    return this.http.get<ReportPage<Record<string, unknown>>>(`${this.base}/procurement/receiving`, {
      params: this.params(q)
    });
  }

  getManagementSummary(fromDate?: string, toDate?: string, branchId?: string): Observable<ManagementSummaryResult> {
    return this.http.get<ManagementSummaryResult>(`${this.base}/management/summary`, {
      params: this.params({ fromDate, toDate, branchId })
    });
  }
}
