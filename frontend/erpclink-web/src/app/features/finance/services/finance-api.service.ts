import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class FinanceApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/finance';

  searchAccounts(query?: string): Observable<{ items: AccountDto[]; totalCount: number }> {
    let params = new HttpParams();
    if (query) params = params.set('query', query);
    return this.http.get<{ items: AccountDto[]; totalCount: number }>(`${this.base}/accounts`, { params });
  }

  createAccount(body: CreateAccountRequest): Observable<AccountDto> {
    return this.http.post<AccountDto>(`${this.base}/accounts`, body);
  }

  searchFiscalYears(): Observable<{ items: FiscalYearDto[] }> {
    return this.http.get<{ items: FiscalYearDto[] }>(`${this.base}/fiscal-years`);
  }

  createFiscalYear(body: CreateFiscalYearRequest): Observable<FiscalYearDto> {
    return this.http.post<FiscalYearDto>(`${this.base}/fiscal-years`, body);
  }

  searchFiscalPeriods(fiscalYearId?: string): Observable<{ items: FiscalPeriodDto[] }> {
    let params = new HttpParams();
    if (fiscalYearId) params = params.set('fiscalYearId', fiscalYearId);
    return this.http.get<{ items: FiscalPeriodDto[] }>(`${this.base}/fiscal-periods`, { params });
  }

  createFiscalPeriod(body: CreateFiscalPeriodRequest): Observable<FiscalPeriodDto> {
    return this.http.post<FiscalPeriodDto>(`${this.base}/fiscal-periods`, body);
  }

  searchJournals(): Observable<{ items: JournalEntryDto[] }> {
    return this.http.get<{ items: JournalEntryDto[] }>(`${this.base}/journals`);
  }

  getJournal(id: string): Observable<JournalEntryDto> {
    return this.http.get<JournalEntryDto>(`${this.base}/journals/${id}`);
  }

  createJournalDraft(body: CreateJournalDraftRequest): Observable<JournalEntryDto> {
    return this.http.post<JournalEntryDto>(`${this.base}/journals`, body);
  }

  replaceJournalLines(id: string, body: ReplaceJournalLinesRequest): Observable<JournalEntryDto> {
    return this.http.put<JournalEntryDto>(`${this.base}/journals/${id}/lines`, body);
  }

  postJournal(id: string, rowVersion: string): Observable<JournalEntryDto> {
    return this.http.post<JournalEntryDto>(`${this.base}/journals/${id}/post`, { rowVersion });
  }

  searchGeneralLedger(fromDate: string, toDate: string): Observable<{ items: GeneralLedgerLineDto[] }> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<{ items: GeneralLedgerLineDto[] }>(`${this.base}/general-ledger`, { params });
  }

  getTrialBalance(fromDate: string, toDate: string): Observable<TrialBalanceResult> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<TrialBalanceResult>(`${this.base}/trial-balance`, { params });
  }

  getArBalance(partyId: string): Observable<PartyBalanceDto> {
    return this.http.get<PartyBalanceDto>(`${this.base}/ar/customers/${partyId}/balance`);
  }

  getArStatement(partyId: string, fromDate?: string, toDate?: string): Observable<StatementResult> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<StatementResult>(`${this.base}/ar/customers/${partyId}/statement`, { params });
  }

  searchArTransactions(partyId?: string): Observable<{ items: SubledgerTransactionDto[]; totalCount: number }> {
    let params = new HttpParams();
    if (partyId) params = params.set('partyId', partyId);
    return this.http.get<{ items: SubledgerTransactionDto[]; totalCount: number }>(`${this.base}/ar/transactions`, {
      params
    });
  }

  getArAging(partyId?: string): Observable<AgingResult> {
    let params = new HttpParams();
    if (partyId) params = params.set('partyId', partyId);
    return this.http.get<AgingResult>(`${this.base}/ar/aging`, { params });
  }

  getApBalance(partyId: string): Observable<PartyBalanceDto> {
    return this.http.get<PartyBalanceDto>(`${this.base}/ap/suppliers/${partyId}/balance`);
  }

  getApStatement(partyId: string, fromDate?: string, toDate?: string): Observable<StatementResult> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<StatementResult>(`${this.base}/ap/suppliers/${partyId}/statement`, { params });
  }

  searchApTransactions(partyId?: string): Observable<{ items: SubledgerTransactionDto[]; totalCount: number }> {
    let params = new HttpParams();
    if (partyId) params = params.set('partyId', partyId);
    return this.http.get<{ items: SubledgerTransactionDto[]; totalCount: number }>(`${this.base}/ap/transactions`, {
      params
    });
  }

  getApAging(partyId?: string): Observable<AgingResult> {
    let params = new HttpParams();
    if (partyId) params = params.set('partyId', partyId);
    return this.http.get<AgingResult>(`${this.base}/ap/aging`, { params });
  }
}

export interface PartyBalanceDto {
  partyId: string;
  subledgerType: string;
  balance: number;
  currencyCode: string;
  partyDisplayName?: string | null;
}

export interface SubledgerTransactionDto {
  id: string;
  partyId: string;
  sourceModule: string;
  sourceType: string;
  sourceId: string;
  eventType: string;
  transactionDate: string;
  debit: number;
  credit: number;
  amount: number;
  description?: string | null;
  status: string;
}

export interface StatementResult {
  partyId: string;
  partyDisplayName?: string | null;
  openingBalance: number;
  closingBalance: number;
  currencyCode: string;
  items: {
    date: string;
    source: string;
    description?: string | null;
    debit: number;
    credit: number;
    runningBalance: number;
    reference: string;
  }[];
}

export interface AgingResult {
  subledgerType: string;
  asOfDate: string;
  totalOutstanding: number;
  buckets: { bucket: string; amount: number; itemCount: number }[];
  items: {
    partyId: string;
    sourceId: string;
    transactionDate: string;
    dueDate?: string | null;
    outstandingAmount: number;
    agingBucket: string;
    daysPastDue: number;
  }[];
}

export interface AccountDto {
  id: string;
  code: string;
  name: string;
  accountType: string;
  isPostable: boolean;
  isActive: boolean;
}

export interface CreateAccountRequest {
  code: string;
  name: string;
  description?: string | null;
  accountType: string;
  isPostable: boolean;
  parentAccountId?: string | null;
}

export interface FiscalYearDto {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  status: string;
}

export interface CreateFiscalYearRequest {
  name: string;
  startDate: string;
  endDate: string;
}

export interface FiscalPeriodDto {
  id: string;
  fiscalYearId: string;
  name: string;
  startDate: string;
  endDate: string;
  status: string;
}

export interface CreateFiscalPeriodRequest {
  fiscalYearId: string;
  name: string;
  startDate: string;
  endDate: string;
}

export interface JournalEntryDto {
  id: string;
  journalNumber: string;
  journalDate: string;
  description?: string | null;
  status: string;
  totalDebit: number;
  totalCredit: number;
  rowVersion: string;
  lines: JournalLineDto[];
}

export interface JournalLineDto {
  id: string;
  accountId: string;
  description?: string | null;
  debit: number;
  credit: number;
  sortOrder: number;
}

export interface JournalLineInput {
  accountId: string;
  description?: string | null;
  debit: number;
  credit: number;
  sortOrder: number;
}

export interface CreateJournalDraftRequest {
  journalDate: string;
  description?: string | null;
  lines?: JournalLineInput[] | null;
}

export interface ReplaceJournalLinesRequest {
  lines: JournalLineInput[];
  rowVersion: string;
}

export interface GeneralLedgerLineDto {
  journalNumber: string;
  journalDate: string;
  accountId: string;
  debit: number;
  credit: number;
}

export interface TrialBalanceResult {
  fromDate: string;
  toDate: string;
  lines: { accountCode: string; accountName: string; totalDebit: number; totalCredit: number }[];
  grandTotalDebit: number;
  grandTotalCredit: number;
}
