export interface InvoiceLineInput {
  serviceId?: string | null;
  packageId?: string | null;
  quantity: number;
  lineDiscountAmount?: number | null;
  sortOrder?: number | null;
}

export interface CreateDraftInvoiceRequest {
  patientId: string;
  medicalVisitId?: string | null;
  invoiceDate: string;
  currencyCode: string;
  notes?: string | null;
  invoiceDiscountAmount?: number | null;
  lines?: InvoiceLineInput[] | null;
}

export interface UpdateDraftInvoiceRequest {
  invoiceDate: string;
  notes?: string | null;
  invoiceDiscountAmount?: number | null;
  rowVersion?: string | null;
}

export interface RecordPaymentRequest {
  invoiceId: string;
  paymentDate: string;
  amount: number;
  method: string;
  referenceNumber?: string | null;
  notes?: string | null;
  invoiceRowVersion?: string | null;
}

export interface InvoiceLineDto {
  id: string;
  source: string;
  serviceId?: string | null;
  packageId?: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  descriptionSnapshot: string;
}

export interface InvoiceDto {
  id: string;
  invoiceNumber?: string | null;
  patientId: string;
  status: string;
  currencyCode: string;
  subTotal: number;
  discountAmount: number;
  totalAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  notes?: string | null;
  invoiceDate: string;
  rowVersion: string;
  lines: InvoiceLineDto[];
}

export interface PagedInvoicesResult {
  items: InvoiceDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PatientOutstandingDto {
  patientId: string;
  outstandingAmount: number;
  currencyCode: string;
}
