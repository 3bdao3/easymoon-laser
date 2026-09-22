import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { ConfirmService } from '../../../shared/components/confirm-dialog/confirm.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/auth/auth.service';
import { Permissions } from '../../../core/permissions/permissions';
import { PatientsApi } from '../services/patients.api';
import { PatientAllergiesPanelComponent } from '../components/patient-allergies-panel/patient-allergies-panel.component';
import { PatientDocumentsPanelComponent } from '../components/patient-documents-panel/patient-documents-panel.component';
import { PatientDocumentsApi } from '../services/patient-documents.api';
import {
  PATIENT_DOCUMENT_CATEGORY_LABELS,
  PatientDocumentCategory,
  PatientDocumentDto,
  PatientDocumentSummaryDto,
  PATIENT_DOCUMENT_TYPE_LABELS
} from '../models/patient-document.models';
import {
  AllergyDto,
  AllergySeverity,
  GENDER_LABELS,
  Gender,
  MedicalHistoryItemDto,
  PatientDto
} from '../models/patient.models';
import { formatAgeLabel, isMinor } from '../utils/patient-age.util';
import { patientInitials, telHref, whatsAppHref } from '../utils/patient-phone.util';
import { MedicalVisitsApiService } from '../../medical-visits/services/medical-visits-api.service';
import {
  CURRENT_ENCOUNTER_KIND_LABELS,
  MedicalVisitListItemDto,
  PATIENT_RELATIONSHIP_LABELS,
  PATIENT_TREATMENT_STATE_LABELS,
  PatientVisitContextDto
} from '../../medical-visits/models/medical-visit.models';
import { PrescriptionsApiService } from '../../prescriptions/services/prescriptions-api.service';
import { PrescriptionListItemDto } from '../../prescriptions/models/prescription.models';
import { AppointmentsApiService } from '../../appointments/services/appointments-api.service';
import { AppointmentListItem } from '../../appointments/models/appointment.models';
import { BillingApiService } from '../../billing/services/billing-api.service';
import { InvoiceDto, PatientOutstandingDto } from '../../billing/models/invoice.models';

export type Patient360Tab =
  | 'overview'
  | 'contact'
  | 'history'
  | 'allergies'
  | 'visits'
  | 'prescriptions'
  | 'appointments'
  | 'financial'
  | 'medications'
  | 'laboratory'
  | 'radiology'
  | 'documents'
  | 'insurance';

export interface PatientTabDef {
  id: Patient360Tab;
  label: string;
  kind: 'active' | 'future';
  permission?: string;
}

export interface TimelineItem {
  id: string;
  at: string;
  kind: string;
  title: string;
  subtitle?: string;
  link?: string | any[];
}

interface LoadState<T> {
  loading: boolean;
  error: boolean;
  items: T[];
  totalCount: number;
}

@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    LoadingSpinnerComponent,
    StatusBadgeComponent,
    EmptyStateComponent,
    HasPermissionDirective,
    PatientAllergiesPanelComponent,
    PatientDocumentsPanelComponent
  ],
  templateUrl: './patient-detail.component.html',
  styleUrl: './patient-detail.component.scss'
})
export class PatientDetailComponent implements OnInit {
  private readonly api = inject(PatientsApi);
  private readonly visitsApi = inject(MedicalVisitsApiService);
  private readonly prescriptionsApi = inject(PrescriptionsApiService);
  private readonly appointmentsApi = inject(AppointmentsApiService);
  private readonly billingApi = inject(BillingApiService);
  private readonly documentsApi = inject(PatientDocumentsApi);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly genderLabel = (gender: Gender): string => GENDER_LABELS[gender];

  readonly loading = signal(true);
  readonly patient = signal<PatientDto | null>(null);
  readonly allergies = signal<AllergyDto[]>([]);
  readonly activeTab = signal<Patient360Tab>('overview');
  readonly phoneCopied = signal(false);
  readonly relatedLoading = signal(false);
  readonly visitContext = signal<PatientVisitContextDto | null>(null);
  readonly visitContextError = signal(false);
  readonly completingTreatment = signal(false);

  readonly relationshipLabel = computed(() => {
    const ctx = this.visitContext();
    return ctx ? PATIENT_RELATIONSHIP_LABELS[ctx.patientRelationship] : null;
  });

  readonly treatmentStateLabel = computed(() => {
    const ctx = this.visitContext();
    return ctx ? PATIENT_TREATMENT_STATE_LABELS[ctx.treatmentState] : null;
  });

  readonly encounterKindLabel = computed(() => {
    const kind = this.visitContext()?.currentEncounterKind;
    return kind ? CURRENT_ENCOUNTER_KIND_LABELS[kind] : null;
  });

  readonly canCompleteTreatment = computed(() => {
    const ctx = this.visitContext();
    return !!ctx?.activeVisit && this.auth.hasPermission(Permissions.MedicalVisitsComplete);
  });

  readonly visits = signal<LoadState<MedicalVisitListItemDto>>({
    loading: false,
    error: false,
    items: [],
    totalCount: 0
  });
  readonly prescriptions = signal<LoadState<PrescriptionListItemDto>>({
    loading: false,
    error: false,
    items: [],
    totalCount: 0
  });
  readonly appointments = signal<LoadState<AppointmentListItem>>({
    loading: false,
    error: false,
    items: [],
    totalCount: 0
  });
  readonly invoices = signal<LoadState<InvoiceDto>>({
    loading: false,
    error: false,
    items: [],
    totalCount: 0
  });
  readonly outstanding = signal<PatientOutstandingDto | null>(null);
  readonly outstandingError = signal(false);
  readonly documentsSummary = signal<PatientDocumentSummaryDto | null>(null);
  readonly documentsSummaryError = signal(false);
  readonly documentsRecent = signal<LoadState<PatientDocumentDto>>({
    loading: false,
    error: false,
    items: [],
    totalCount: 0
  });

  private patientId = '';
  private relatedLoaded = false;

  readonly tabs: PatientTabDef[] = [
    { id: 'overview', label: 'نظرة عامة', kind: 'active' },
    { id: 'contact', label: 'التواصل', kind: 'active' },
    { id: 'history', label: 'التاريخ الطبي', kind: 'active' },
    { id: 'allergies', label: 'الحساسيات', kind: 'active' },
    {
      id: 'visits',
      label: 'الزيارات',
      kind: 'active',
      permission: Permissions.MedicalVisitsView
    },
    {
      id: 'prescriptions',
      label: 'الوصفات',
      kind: 'active',
      permission: Permissions.PrescriptionsView
    },
    {
      id: 'appointments',
      label: 'المواعيد',
      kind: 'active',
      permission: Permissions.AppointmentsView
    },
    {
      id: 'financial',
      label: 'المالي',
      kind: 'active',
      permission: Permissions.FinanceInvoicesView
    },
    { id: 'medications', label: 'الأدوية', kind: 'future' },
    { id: 'laboratory', label: 'المعمل', kind: 'future' },
    { id: 'radiology', label: 'الأشعة', kind: 'future' },
    {
      id: 'documents',
      label: 'المستندات',
      kind: 'active',
      permission: Permissions.PatientsDocumentsView
    },
    { id: 'insurance', label: 'التأمين', kind: 'future' }
  ];

  readonly visibleTabs = computed(() =>
    this.tabs.filter((t) => !t.permission || this.auth.hasPermission(t.permission))
  );

  readonly ageLabel = computed(() => {
    const p = this.patient();
    return p ? formatAgeLabel(p.dateOfBirth) : '—';
  });

  readonly patientIsMinor = computed(() => {
    const p = this.patient();
    return p ? isMinor(p.dateOfBirth) : false;
  });

  readonly initials = computed(() => {
    const p = this.patient();
    return p ? patientInitials(p.fullName) : '?';
  });

  readonly activeAllergyCount = computed(
    () => this.allergies().filter((a) => a.isActive).length
  );

  readonly criticalAllergyCount = computed(
    () =>
      this.allergies().filter(
        (a) =>
          a.isActive &&
          (a.severity === AllergySeverity.Severe ||
            a.severity === AllergySeverity.LifeThreatening)
      ).length
  );

  readonly timeline = computed((): TimelineItem[] => {
    const p = this.patient();
    if (!p) {
      return [];
    }
    const items: TimelineItem[] = [
      {
        id: `reg-${p.id}`,
        at: p.createdAtUtc,
        kind: 'registration',
        title: 'تسجيل المريض',
        subtitle: `رقم الملف ${p.patientNumber}`
      }
    ];

    for (const h of p.medicalHistory ?? []) {
      items.push({
        id: `hist-${h.id}`,
        at: h.createdAtUtc,
        kind: 'history',
        title: h.category || 'تاريخ طبي',
        subtitle: h.description
      });
    }

    for (const a of this.allergies()) {
      items.push({
        id: `alg-${a.id}`,
        at: a.createdAtUtc,
        kind: 'allergy',
        title: `حساسية: ${a.name}`,
        subtitle: a.isActive ? 'نشطة' : 'موقوفة'
      });
    }

    for (const v of this.visits().items) {
      items.push({
        id: `vis-${v.id}`,
        at: v.visitDate,
        kind: 'visit',
        title: `زيارة ${v.visitNumber}`,
        subtitle: v.chiefComplaint || v.diagnosisNotes || v.status,
        link: ['/app/medical-visits', v.id]
      });
    }

    for (const rx of this.prescriptions().items) {
      items.push({
        id: `rx-${rx.id}`,
        at: rx.prescriptionDate,
        kind: 'prescription',
        title: `وصفة ${rx.prescriptionNumber}`,
        subtitle: rx.status,
        link: ['/app/prescriptions', rx.id]
      });
    }

    for (const ap of this.appointments().items) {
      items.push({
        id: `ap-${ap.id}`,
        at: `${ap.appointmentDate}T${ap.startTime}`,
        kind: 'appointment',
        title: `موعد ${ap.appointmentNumber}`,
        subtitle: ap.status,
        link: ['/app/appointments', ap.id]
      });
    }

    for (const inv of this.invoices().items) {
      items.push({
        id: `inv-${inv.id}`,
        at: inv.invoiceDate,
        kind: 'invoice',
        title: inv.invoiceNumber ? `فاتورة ${inv.invoiceNumber}` : 'فاتورة',
        subtitle: `${inv.status} — ${inv.outstandingAmount} ${inv.currencyCode}`,
        link: ['/app/billing/invoices', inv.id]
      });
    }

    for (const doc of this.documentsRecent().items) {
      const at = doc.documentDate
        ? `${doc.documentDate}T12:00:00Z`
        : doc.createdAtUtc;
      items.push({
        id: `doc-${doc.id}`,
        at,
        kind: 'document',
        title: doc.originalFileName,
        subtitle: `${PATIENT_DOCUMENT_TYPE_LABELS[doc.documentType] ?? 'مستند'} — ${
          PATIENT_DOCUMENT_CATEGORY_LABELS[doc.category] ?? ''
        }`
      });
    }

    return items.sort((a, b) => (a.at < b.at ? 1 : a.at > b.at ? -1 : 0)).slice(0, 12);
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/patients']);
      return;
    }
    this.patientId = id;
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.relatedLoaded = false;
    this.api.getById(this.patientId).subscribe({
      next: (p) => {
        this.patient.set(p);
        this.allergies.set(p.allergies ?? []);
        this.loading.set(false);
        this.loadRelatedOverview();
      },
      error: () => void this.router.navigate(['/app/patients'])
    });
  }

  selectTab(tab: Patient360Tab): void {
    this.activeTab.set(tab);
    if (tab === 'visits') {
      this.ensureVisits(20);
    } else if (tab === 'prescriptions') {
      this.ensurePrescriptions(20);
    } else if (tab === 'appointments') {
      this.ensureAppointments(20);
    } else if (tab === 'financial') {
      this.ensureFinancial(20);
    }
  }

  onAllergiesChange(list: AllergyDto[]): void {
    this.allergies.set(list);
  }

  telLink(phone: string | null | undefined): string | null {
    return telHref(phone);
  }

  waLink(phone: string | null | undefined): string | null {
    return whatsAppHref(phone);
  }

  async copyPhone(phone: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(phone);
      this.phoneCopied.set(true);
      this.toast.success('تم نسخ رقم الهاتف');
      setTimeout(() => this.phoneCopied.set(false), 1500);
    } catch {
      this.toast.error('تعذر نسخ الرقم');
    }
  }

  medicalHistory(): MedicalHistoryItemDto[] {
    return this.patient()?.medicalHistory ?? [];
  }

  canCreateAppointment(): boolean {
    return this.auth.hasPermission(Permissions.AppointmentsCreate);
  }

  canCreateVisit(): boolean {
    return this.auth.hasPermission(Permissions.MedicalVisitsCreate);
  }

  authHasFinance(): boolean {
    return this.auth.hasPermission(Permissions.FinanceInvoicesView);
  }

  authHasDocuments(): boolean {
    return this.auth.hasPermission(Permissions.PatientsDocumentsView);
  }

  authHasVisits(): boolean {
    return this.auth.hasPermission(Permissions.MedicalVisitsView);
  }

  async completeTreatment(): Promise<void> {
    const active = this.visitContext()?.activeVisit;
    if (!active) {
      this.toast.error('لا توجد زيارة نشطة لإكمال العلاج.');
      return;
    }

    const ok = await this.confirm.confirm({
      title: 'إكمال العلاج',
      message:
        'هل أنت متأكد أن العلاج الحالي قد اكتمل؟ لن يتم حذف المريض أو زياراته أو مستنداته — سيتم إكمال الزيارة النشطة فقط.',
      variant: 'default'
    });
    if (!ok) {
      return;
    }

    this.completingTreatment.set(true);
    this.visitsApi.complete(active.id).subscribe({
      next: () => {
        this.toast.success('تم إكمال الزيارة / العلاج الحالي');
        this.completingTreatment.set(false);
        this.refreshVisitContext();
        this.ensureVisits(5);
      },
      error: () => this.completingTreatment.set(false)
    });
  }

  documentCategoryLabel(category: PatientDocumentCategory): string {
    return PATIENT_DOCUMENT_CATEGORY_LABELS[category] ?? String(category);
  }

  onDocumentsChange(list: PatientDocumentDto[]): void {
    this.documentsRecent.update((s) => ({
      ...s,
      items: list.slice(0, 5),
      totalCount: Math.max(s.totalCount, list.length)
    }));
    if (this.authHasDocuments()) {
      this.documentsApi.summary(this.patientId).subscribe({
        next: (summary) => {
          this.documentsSummary.set(summary);
          this.documentsSummaryError.set(false);
        },
        error: () => this.documentsSummaryError.set(true)
      });
    }
  }

  async togglePatientActive(): Promise<void> {
    const p = this.patient();
    if (!p) {
      return;
    }
    const activate = !p.isActive;
    const ok = await this.confirm.confirm({
      title: activate ? 'تفعيل' : 'إيقاف',
      message: activate ? 'تفعيل المريض؟' : 'إيقاف المريض؟',
      variant: activate ? 'default' : 'danger'
    });
    if (!ok) {
      return;
    }
    const req = activate ? this.api.activate(p.id) : this.api.deactivate(p.id);
    req.subscribe({
      next: () => {
        this.toast.success(activate ? 'تم التفعيل' : 'تم الإيقاف');
        this.reload();
      }
    });
  }

  private loadRelatedOverview(): void {
    if (this.relatedLoaded) {
      return;
    }
    this.relatedLoaded = true;
    this.relatedLoading.set(true);

    const visits$ = this.auth.hasPermission(Permissions.MedicalVisitsView)
      ? this.visitsApi.patientHistory(this.patientId, { page: 1, pageSize: 5 }).pipe(
          catchError(() => of(null))
        )
      : of(undefined);

    const visitContext$ = this.auth.hasPermission(Permissions.MedicalVisitsView)
      ? this.visitsApi.patientVisitContext(this.patientId).pipe(catchError(() => of(null)))
      : of(undefined);

    const rx$ = this.auth.hasPermission(Permissions.PrescriptionsView)
      ? this.prescriptionsApi.patientHistory(this.patientId, { page: 1, pageSize: 5 }).pipe(
          catchError(() => of(null))
        )
      : of(undefined);

    const appt$ = this.auth.hasPermission(Permissions.AppointmentsView)
      ? this.appointmentsApi
          .search({ patientId: this.patientId, page: 1, pageSize: 5, sortDescending: true })
          .pipe(catchError(() => of(null)))
      : of(undefined);

    const inv$ = this.auth.hasPermission(Permissions.FinanceInvoicesView)
      ? this.billingApi.patientInvoices(this.patientId, { page: 1, pageSize: 5 }).pipe(
          catchError(() => of(null))
        )
      : of(undefined);

    const out$ = this.auth.hasPermission(Permissions.FinanceInvoicesView)
      ? this.billingApi.patientOutstanding(this.patientId).pipe(catchError(() => of(null)))
      : of(undefined);

    const docSummary$ = this.auth.hasPermission(Permissions.PatientsDocumentsView)
      ? this.documentsApi.summary(this.patientId).pipe(catchError(() => of(null)))
      : of(undefined);

    const docRecent$ = this.auth.hasPermission(Permissions.PatientsDocumentsView)
      ? this.documentsApi
          .search(this.patientId, { page: 1, pageSize: 5, isActive: true })
          .pipe(catchError(() => of(null)))
      : of(undefined);

    forkJoin({
      visits: visits$,
      visitContext: visitContext$,
      rx: rx$,
      appt: appt$,
      inv: inv$,
      out: out$,
      docSummary: docSummary$,
      docRecent: docRecent$
    }).subscribe({
      next: ({ visits, visitContext, rx, appt, inv, out, docSummary, docRecent }) => {
        if (visits === null) {
          this.visits.set({ loading: false, error: true, items: [], totalCount: 0 });
        } else if (visits) {
          this.visits.set({
            loading: false,
            error: false,
            items: visits.items,
            totalCount: visits.totalCount
          });
        }

        if (visitContext === null) {
          this.visitContextError.set(true);
          this.visitContext.set(null);
        } else if (visitContext) {
          this.visitContextError.set(false);
          this.visitContext.set(visitContext);
        }

        if (rx === null) {
          this.prescriptions.set({ loading: false, error: true, items: [], totalCount: 0 });
        } else if (rx) {
          this.prescriptions.set({
            loading: false,
            error: false,
            items: rx.items,
            totalCount: rx.totalCount
          });
        }

        if (appt === null) {
          this.appointments.set({ loading: false, error: true, items: [], totalCount: 0 });
        } else if (appt) {
          this.appointments.set({
            loading: false,
            error: false,
            items: appt.items,
            totalCount: appt.totalCount
          });
        }

        if (inv === null) {
          this.invoices.set({ loading: false, error: true, items: [], totalCount: 0 });
        } else if (inv) {
          this.invoices.set({
            loading: false,
            error: false,
            items: inv.items,
            totalCount: inv.totalCount
          });
        }

        if (out === null) {
          this.outstandingError.set(true);
          this.outstanding.set(null);
        } else if (out) {
          this.outstandingError.set(false);
          this.outstanding.set(out);
        }

        if (docSummary === null) {
          this.documentsSummaryError.set(true);
          this.documentsSummary.set(null);
        } else if (docSummary) {
          this.documentsSummaryError.set(false);
          this.documentsSummary.set(docSummary);
        }

        if (docRecent === null) {
          this.documentsRecent.set({ loading: false, error: true, items: [], totalCount: 0 });
        } else if (docRecent) {
          this.documentsRecent.set({
            loading: false,
            error: false,
            items: docRecent.items,
            totalCount: docRecent.totalCount
          });
        }

        this.relatedLoading.set(false);
      },
      error: () => this.relatedLoading.set(false)
    });
  }

  private refreshVisitContext(): void {
    if (!this.auth.hasPermission(Permissions.MedicalVisitsView)) {
      return;
    }
    this.visitsApi.patientVisitContext(this.patientId).subscribe({
      next: (ctx) => {
        this.visitContext.set(ctx);
        this.visitContextError.set(false);
      },
      error: () => this.visitContextError.set(true)
    });
  }

  private ensureVisits(pageSize: number): void {
    if (!this.auth.hasPermission(Permissions.MedicalVisitsView)) {
      return;
    }
    this.visits.update((s) => ({ ...s, loading: true, error: false }));
    this.visitsApi.patientHistory(this.patientId, { page: 1, pageSize }).subscribe({
      next: (r) =>
        this.visits.set({
          loading: false,
          error: false,
          items: r.items,
          totalCount: r.totalCount
        }),
      error: () =>
        this.visits.set({ loading: false, error: true, items: [], totalCount: 0 })
    });
  }

  private ensurePrescriptions(pageSize: number): void {
    if (!this.auth.hasPermission(Permissions.PrescriptionsView)) {
      return;
    }
    this.prescriptions.update((s) => ({ ...s, loading: true, error: false }));
    this.prescriptionsApi.patientHistory(this.patientId, { page: 1, pageSize }).subscribe({
      next: (r) =>
        this.prescriptions.set({
          loading: false,
          error: false,
          items: r.items,
          totalCount: r.totalCount
        }),
      error: () =>
        this.prescriptions.set({ loading: false, error: true, items: [], totalCount: 0 })
    });
  }

  private ensureAppointments(pageSize: number): void {
    if (!this.auth.hasPermission(Permissions.AppointmentsView)) {
      return;
    }
    this.appointments.update((s) => ({ ...s, loading: true, error: false }));
    this.appointmentsApi
      .search({ patientId: this.patientId, page: 1, pageSize, sortDescending: true })
      .subscribe({
        next: (r) =>
          this.appointments.set({
            loading: false,
            error: false,
            items: r.items,
            totalCount: r.totalCount
          }),
        error: () =>
          this.appointments.set({ loading: false, error: true, items: [], totalCount: 0 })
      });
  }

  private ensureFinancial(pageSize: number): void {
    if (!this.auth.hasPermission(Permissions.FinanceInvoicesView)) {
      return;
    }
    this.invoices.update((s) => ({ ...s, loading: true, error: false }));
    this.billingApi.patientInvoices(this.patientId, { page: 1, pageSize }).subscribe({
      next: (r) =>
        this.invoices.set({
          loading: false,
          error: false,
          items: r.items,
          totalCount: r.totalCount
        }),
      error: () =>
        this.invoices.set({ loading: false, error: true, items: [], totalCount: 0 })
    });
    this.billingApi.patientOutstanding(this.patientId).subscribe({
      next: (o) => {
        this.outstanding.set(o);
        this.outstandingError.set(false);
      },
      error: () => {
        this.outstanding.set(null);
        this.outstandingError.set(true);
      }
    });
  }
}
