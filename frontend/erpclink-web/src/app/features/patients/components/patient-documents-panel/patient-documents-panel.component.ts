import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  OnDestroy,
  effect,
  inject,
  input,
  output,
  signal
} from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { ConfirmService } from '../../../../shared/components/confirm-dialog/confirm.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Permissions } from '../../../../core/permissions/permissions';
import { trimToNull } from '../../../../shared/utils/form.util';
import { PatientDocumentsApi } from '../../services/patient-documents.api';
import {
  PATIENT_DOCUMENT_CATEGORY_LABELS,
  PATIENT_DOCUMENT_TYPE_LABELS,
  PatientDocumentCategory,
  PatientDocumentDto,
  PatientDocumentType,
  documentTypesForCategory,
  formatFileSizeBytes,
  isImageContentType,
  isPdfContentType
} from '../../models/patient-document.models';

@Component({
  selector: 'app-patient-documents-panel',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    LoadingSpinnerComponent,
    StatusBadgeComponent,
    EmptyStateComponent,
    HasPermissionDirective
  ],
  templateUrl: './patient-documents-panel.component.html',
  styleUrl: './patient-documents-panel.component.scss'
})
export class PatientDocumentsPanelComponent implements OnDestroy {
  private readonly api = inject(PatientDocumentsApi);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly patientId = input.required<string>();
  readonly patientActive = input(true);

  readonly documentsChange = output<PatientDocumentDto[]>();

  readonly permissions = Permissions;
  readonly categoryLabels = PATIENT_DOCUMENT_CATEGORY_LABELS;
  readonly typeLabels = PATIENT_DOCUMENT_TYPE_LABELS;
  readonly formatSize = formatFileSizeBytes;

  readonly loading = signal(false);
  readonly error = signal(false);
  readonly items = signal<PatientDocumentDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);

  readonly showUpload = signal(false);
  readonly uploadSaving = signal(false);
  readonly duplicateConflict = signal(false);
  readonly allowPossibleDuplicate = signal(false);
  readonly selectedFile = signal<File | null>(null);

  readonly editingId = signal<string | null>(null);
  readonly editSaving = signal(false);

  readonly previewOpen = signal(false);
  readonly previewLoading = signal(false);
  readonly previewDoc = signal<PatientDocumentDto | null>(null);
  readonly previewBlobUrl = signal<string | null>(null);
  readonly previewSafeUrl = signal<SafeResourceUrl | null>(null);
  readonly previewMode = signal<'image' | 'pdf' | 'download'>('download');

  readonly categoryOptions = (
    Object.entries(PATIENT_DOCUMENT_CATEGORY_LABELS) as [string, string][]
  ).map(([value, label]) => ({
    value: Number(value) as PatientDocumentCategory,
    label
  }));

  readonly filterForm = new FormGroup({
    query: new FormControl('', { nonNullable: true }),
    category: new FormControl<'' | PatientDocumentCategory>('', { nonNullable: true }),
    documentType: new FormControl<'' | PatientDocumentType>('', { nonNullable: true }),
    dateFrom: new FormControl('', { nonNullable: true }),
    dateTo: new FormControl('', { nonNullable: true }),
    isActive: new FormControl<'all' | 'active' | 'inactive'>('active', { nonNullable: true }),
    uploadedBy: new FormControl('', { nonNullable: true })
  });

  readonly uploadForm = new FormGroup({
    category: new FormControl(PatientDocumentCategory.MedicalImaging, {
      nonNullable: true,
      validators: [Validators.required]
    }),
    documentType: new FormControl(PatientDocumentType.XRay, {
      nonNullable: true,
      validators: [Validators.required]
    }),
    documentDate: new FormControl('', { nonNullable: true }),
    description: new FormControl('', { nonNullable: true }),
    medicalVisitId: new FormControl('', { nonNullable: true })
  });

  readonly editForm = new FormGroup({
    category: new FormControl(PatientDocumentCategory.MedicalImaging, {
      nonNullable: true,
      validators: [Validators.required]
    }),
    documentType: new FormControl(PatientDocumentType.XRay, {
      nonNullable: true,
      validators: [Validators.required]
    }),
    documentDate: new FormControl('', { nonNullable: true }),
    description: new FormControl('', { nonNullable: true }),
    medicalVisitId: new FormControl('', { nonNullable: true })
  });

  constructor() {
    effect(() => {
      const id = this.patientId();
      if (id) {
        this.search(1);
      }
    });

    this.uploadForm.controls.category.valueChanges.subscribe((cat) => {
      const types = documentTypesForCategory(cat);
      const current = this.uploadForm.controls.documentType.value;
      if (!types.includes(current)) {
        this.uploadForm.controls.documentType.setValue(types[0] ?? PatientDocumentType.Other);
      }
    });

    this.editForm.controls.category.valueChanges.subscribe((cat) => {
      const types = documentTypesForCategory(cat);
      const current = this.editForm.controls.documentType.value;
      if (!types.includes(current)) {
        this.editForm.controls.documentType.setValue(types[0] ?? PatientDocumentType.Other);
      }
    });

    effect(() => {
      if (this.patientActive()) {
        this.uploadForm.enable({ emitEvent: false });
        this.editForm.enable({ emitEvent: false });
      } else {
        this.uploadForm.disable({ emitEvent: false });
        this.editForm.disable({ emitEvent: false });
      }
    });
  }

  ngOnDestroy(): void {
    this.revokePreviewUrl();
  }

  uploadTypeOptions(): { value: PatientDocumentType; label: string }[] {
    const cat = this.uploadForm.controls.category.value;
    return documentTypesForCategory(cat).map((value) => ({
      value,
      label: this.typeLabels[value]
    }));
  }

  editTypeOptions(): { value: PatientDocumentType; label: string }[] {
    const cat = this.editForm.controls.category.value;
    return documentTypesForCategory(cat).map((value) => ({
      value,
      label: this.typeLabels[value]
    }));
  }

  filterTypeOptions(): { value: PatientDocumentType; label: string }[] {
    const raw = this.filterForm.controls.category.value;
    if (raw === '') {
      return (Object.entries(PATIENT_DOCUMENT_TYPE_LABELS) as [string, string][]).map(
        ([value, label]) => ({
          value: Number(value) as PatientDocumentType,
          label
        })
      );
    }
    return documentTypesForCategory(raw).map((value) => ({
      value,
      label: this.typeLabels[value]
    }));
  }

  onFilterCategoryChange(): void {
    const types = this.filterTypeOptions();
    const current = this.filterForm.controls.documentType.value;
    if (current !== '' && !types.some((t) => t.value === current)) {
      this.filterForm.controls.documentType.setValue('');
    }
  }

  search(page = this.page()): void {
    this.page.set(page);
    this.loading.set(true);
    this.error.set(false);
    const f = this.filterForm.getRawValue();
    const isActive =
      f.isActive === 'all' ? undefined : f.isActive === 'active' ? true : false;

    this.api
      .search(this.patientId(), {
        query: trimToNull(f.query) ?? undefined,
        category: f.category === '' ? undefined : f.category,
        documentType: f.documentType === '' ? undefined : f.documentType,
        dateFrom: trimToNull(f.dateFrom) ?? undefined,
        dateTo: trimToNull(f.dateTo) ?? undefined,
        isActive,
        uploadedBy: trimToNull(f.uploadedBy) ?? undefined,
        page: this.page(),
        pageSize: this.pageSize()
      })
      .subscribe({
        next: (r) => {
          this.items.set(r.items);
          this.totalCount.set(r.totalCount);
          this.loading.set(false);
          this.documentsChange.emit(r.items);
        },
        error: () => {
          this.loading.set(false);
          this.error.set(true);
        }
      });
  }

  resetFilters(): void {
    this.filterForm.reset({
      query: '',
      category: '',
      documentType: '',
      dateFrom: '',
      dateTo: '',
      isActive: 'active',
      uploadedBy: ''
    });
    this.search(1);
  }

  openUpload(): void {
    this.duplicateConflict.set(false);
    this.allowPossibleDuplicate.set(false);
    this.selectedFile.set(null);
    this.uploadForm.reset({
      category: PatientDocumentCategory.MedicalImaging,
      documentType: PatientDocumentType.XRay,
      documentDate: '',
      description: '',
      medicalVisitId: ''
    });
    this.showUpload.set(true);
  }

  closeUpload(): void {
    this.showUpload.set(false);
    this.duplicateConflict.set(false);
    this.allowPossibleDuplicate.set(false);
    this.selectedFile.set(null);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile.set(file);
    this.duplicateConflict.set(false);
    this.allowPossibleDuplicate.set(false);
  }

  submitUpload(): void {
    if (!this.patientActive()) {
      this.toast.error('لا يمكن رفع مستندات لمريض موقوف');
      return;
    }
    const file = this.selectedFile();
    if (!file) {
      this.toast.error('اختر ملفاً للرفع');
      return;
    }
    this.uploadForm.markAllAsTouched();
    if (this.uploadForm.invalid) {
      return;
    }
    const v = this.uploadForm.getRawValue();
    const form = new FormData();
    form.append('file', file);
    form.append('category', String(v.category));
    form.append('documentType', String(v.documentType));
    if (v.documentDate) {
      form.append('documentDate', v.documentDate);
    }
    const desc = trimToNull(v.description);
    if (desc) {
      form.append('description', desc);
    }
    const visitId = trimToNull(v.medicalVisitId);
    if (visitId) {
      form.append('medicalVisitId', visitId);
    }
    if (this.allowPossibleDuplicate()) {
      form.append('allowPossibleDuplicate', 'true');
    }

    this.uploadSaving.set(true);
    this.api.upload(this.patientId(), form).subscribe({
      next: (result) => {
        this.uploadSaving.set(false);
        this.toast.success('تم رفع المستند');
        if (result.possibleDuplicateWarning) {
          this.toast.error('تنبيه: قد يكون المستند مكرراً');
        }
        this.closeUpload();
        this.search(1);
      },
      error: (err: HttpErrorResponse) => {
        this.uploadSaving.set(false);
        if (err.status === 409) {
          this.duplicateConflict.set(true);
          this.toast.error('يوجد مستند مشابه — يمكنك تأكيد الرغبة في الرفع');
          return;
        }
        this.toast.error('تعذر رفع المستند');
      }
    });
  }

  startEdit(doc: PatientDocumentDto): void {
    this.editingId.set(doc.id);
    this.editForm.setValue({
      category: doc.category,
      documentType: doc.documentType,
      documentDate: doc.documentDate ?? '',
      description: doc.description ?? '',
      medicalVisitId: doc.medicalVisitId ?? ''
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(): void {
    if (!this.patientActive()) {
      this.toast.error('لا يمكن تعديل مستندات مريض موقوف');
      return;
    }
    const id = this.editingId();
    if (!id) {
      return;
    }
    this.editForm.markAllAsTouched();
    if (this.editForm.invalid) {
      return;
    }
    const v = this.editForm.getRawValue();
    if (!documentTypesForCategory(v.category).includes(v.documentType)) {
      this.toast.error('نوع المستند لا يطابق التصنيف');
      return;
    }
    this.editSaving.set(true);
    this.api
      .updateMetadata(this.patientId(), id, {
        category: v.category,
        documentType: v.documentType,
        documentDate: trimToNull(v.documentDate),
        description: trimToNull(v.description),
        medicalVisitId: trimToNull(v.medicalVisitId)
      })
      .subscribe({
        next: () => {
          this.editSaving.set(false);
          this.toast.success('تم تحديث بيانات المستند');
          this.cancelEdit();
          this.search(this.page());
        },
        error: () => {
          this.editSaving.set(false);
          this.toast.error('تعذر تحديث المستند');
        }
      });
  }

  async deactivate(doc: PatientDocumentDto): Promise<void> {
    const ok = await this.confirm.confirm({
      title: 'إيقاف مستند',
      message: `إيقاف "${doc.originalFileName}"؟ لن يظهر في القائمة النشطة.`,
      variant: 'danger'
    });
    if (!ok) {
      return;
    }
    this.api.deactivate(this.patientId(), doc.id).subscribe({
      next: () => {
        this.toast.success('تم إيقاف المستند');
        this.search(this.page());
      },
      error: () => this.toast.error('تعذر إيقاف المستند')
    });
  }

  openPreview(doc: PatientDocumentDto): void {
    this.revokePreviewUrl();
    this.previewDoc.set(doc);
    this.previewOpen.set(true);
    this.previewLoading.set(true);

    const mode = isImageContentType(doc.contentType)
      ? 'image'
      : isPdfContentType(doc.contentType, doc.originalFileName)
        ? 'pdf'
        : 'download';
    this.previewMode.set(mode);

    if (mode === 'download') {
      this.previewLoading.set(false);
      return;
    }

    this.api.download(this.patientId(), doc.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        this.previewBlobUrl.set(url);
        this.previewSafeUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(url));
        this.previewLoading.set(false);
      },
      error: () => {
        this.previewLoading.set(false);
        this.toast.error('تعذر تحميل المعاينة');
        this.closePreview();
      }
    });
  }

  closePreview(): void {
    this.previewOpen.set(false);
    this.previewDoc.set(null);
    this.revokePreviewUrl();
  }

  downloadCurrent(doc?: PatientDocumentDto): void {
    const d = doc ?? this.previewDoc();
    if (!d) {
      return;
    }
    this.api.download(this.patientId(), d.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = d.originalFileName;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.toast.error('تعذر تنزيل الملف')
    });
  }

  categoryLabel(cat: PatientDocumentCategory): string {
    return this.categoryLabels[cat] ?? String(cat);
  }

  typeLabel(type: PatientDocumentType): string {
    return this.typeLabels[type] ?? String(type);
  }

  private revokePreviewUrl(): void {
    const url = this.previewBlobUrl();
    if (url) {
      URL.revokeObjectURL(url);
    }
    this.previewBlobUrl.set(null);
    this.previewSafeUrl.set(null);
  }
}
