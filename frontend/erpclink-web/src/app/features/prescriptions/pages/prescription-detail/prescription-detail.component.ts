import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PrescriptionsApiService } from '../../services/prescriptions-api.service';
import {
  isPrescriptionEditable,
  PrescriptionDto,
  PrescriptionItemDto
} from '../../models/prescription.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { MedicationPickerComponent } from '../../components/medication-picker/medication-picker.component';
import { MedicationDto } from '../../../medications/models/medication.models';
import { formatDateOnly, formatDateTimeUtc } from '../../../../shared/utils/date.utils';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ConfirmService } from '../../../../shared/components/confirm-dialog/confirm.service';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';

@Component({
  selector: 'app-prescription-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    LoadingSpinnerComponent,
    MedicationPickerComponent,
    HasPermissionDirective
  ],
  templateUrl: './prescription-detail.component.html',
  styleUrls: [
    './prescription-detail.component.scss',
    '../../../medical-visits/_feature-layout.scss',
    '../../_feature-layout.scss'
  ]
})
export class PrescriptionDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(PrescriptionsApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly permissions = Permissions;
  readonly formatDate = formatDateOnly;
  readonly formatDateTime = formatDateTimeUtc;

  readonly loading = signal(true);
  readonly savingNotes = signal(false);
  readonly itemBusy = signal(false);
  readonly prescription = signal<PrescriptionDto | null>(null);
  readonly pickedMedication = signal<MedicationDto | null>(null);
  readonly editingItem = signal<PrescriptionItemDto | null>(null);

  readonly editable = computed(() => {
    const rx = this.prescription();
    return rx ? isPrescriptionEditable(rx.status) : false;
  });

  readonly notesForm = this.fb.nonNullable.group({
    notes: ['']
  });

  readonly itemForm = this.fb.nonNullable.group({
    dosage: ['', Validators.required],
    frequency: ['', Validators.required],
    duration: [''],
    route: [''],
    instructions: [''],
    quantity: [''],
    notes: [''],
    sortOrder: [0]
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.load(id);
    } else {
      this.loading.set(false);
    }
  }

  load(id: string): void {
    this.loading.set(true);
    this.api.getById(id).subscribe({
      next: (dto) => {
        this.prescription.set(dto);
        this.notesForm.patchValue({ notes: dto.notes ?? '' });
        this.applyNotesState();
        this.editingItem.set(null);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.toast.error(this.errors.resolveMessage(err));
      }
    });
  }

  saveNotes(): void {
    const rx = this.prescription();
    if (!rx || !this.editable() || this.savingNotes()) {
      return;
    }

    this.savingNotes.set(true);
    this.api
      .update(rx.id, {
        notes: this.notesForm.getRawValue().notes || null,
        rowVersion: rx.rowVersion
      })
      .subscribe({
        next: (updated) => {
          this.prescription.set(updated);
          this.notesForm.patchValue({ notes: updated.notes ?? '' });
          this.savingNotes.set(false);
          this.toast.success('تم حفظ الملاحظات.');
        },
        error: (err: HttpErrorResponse) => {
          this.savingNotes.set(false);
          this.toast.error(this.errors.resolveMessage(err));
          if (err.status === 409) {
            this.load(rx.id);
          }
        }
      });
  }

  onMedicationPicked(med: MedicationDto): void {
    this.pickedMedication.set(med);
    this.editingItem.set(null);
    this.itemForm.reset({ sortOrder: this.prescription()?.items.length ?? 0 });
  }

  startEditItem(item: PrescriptionItemDto): void {
    this.editingItem.set(item);
    this.pickedMedication.set(null);
    this.itemForm.patchValue({
      dosage: item.dosage,
      frequency: item.frequency,
      duration: item.duration ?? '',
      route: item.route ?? '',
      instructions: item.instructions ?? '',
      quantity: item.quantity != null ? String(item.quantity) : '',
      notes: item.notes ?? '',
      sortOrder: item.sortOrder
    });
  }

  cancelItemEdit(): void {
    this.editingItem.set(null);
    this.pickedMedication.set(null);
    this.itemForm.reset();
  }

  submitItem(): void {
    const rx = this.prescription();
    if (!rx || !this.editable() || this.itemBusy() || this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }

    const raw = this.itemForm.getRawValue();
    const qty = raw.quantity.trim() ? Number(raw.quantity) : null;
    const editing = this.editingItem();

    this.itemBusy.set(true);

    if (editing) {
      this.api
        .updateItem(rx.id, editing.id, {
          dosage: raw.dosage,
          frequency: raw.frequency,
          duration: raw.duration || null,
          route: raw.route || null,
          instructions: raw.instructions || null,
          quantity: qty !== null && !Number.isNaN(qty) ? qty : null,
          notes: raw.notes || null,
          sortOrder: raw.sortOrder,
          rowVersion: rx.rowVersion
        })
        .subscribe({
          next: (updated) => this.onItemMutationSuccess(updated),
          error: (err) => this.onItemMutationError(err, rx.id)
        });
    } else {
      const med = this.pickedMedication();
      if (!med) {
        this.itemBusy.set(false);
        this.toast.warning('اختر دواءً لإضافة بند جديد.');
        return;
      }
      this.api
        .addItem(rx.id, {
          medicationId: med.id,
          dosage: raw.dosage,
          frequency: raw.frequency,
          duration: raw.duration || null,
          route: raw.route || null,
          instructions: raw.instructions || null,
          quantity: qty !== null && !Number.isNaN(qty) ? qty : null,
          notes: raw.notes || null,
          sortOrder: raw.sortOrder
        })
        .subscribe({
          next: (updated) => this.onItemMutationSuccess(updated),
          error: (err) => this.onItemMutationError(err, rx.id)
        });
    }
  }

  async removeItem(item: PrescriptionItemDto): Promise<void> {
    const rx = this.prescription();
    if (!rx || !this.editable()) {
      return;
    }
    const ok = await this.confirm.confirm({
      title: 'حذف البند',
      message: `حذف «${item.medicationNameSnapshot}» من الوصفة؟`,
      confirmLabel: 'حذف',
      variant: 'danger'
    });
    if (!ok) {
      return;
    }

    this.itemBusy.set(true);
    this.api.removeItem(rx.id, item.id, rx.rowVersion).subscribe({
      next: (updated) => this.onItemMutationSuccess(updated),
      error: (err) => this.onItemMutationError(err, rx.id)
    });
  }

  async issuePrescription(): Promise<void> {
    const rx = this.prescription();
    if (!rx) {
      return;
    }
    const ok = await this.confirm.confirm({
      title: 'إصدار الوصفة',
      message: 'بعد الإصدار لن يمكن تعديل البنود. متابعة؟',
      confirmLabel: 'إصدار'
    });
    if (!ok) {
      return;
    }

    this.api.issue(rx.id).subscribe({
      next: () => {
        this.toast.success('تم إصدار الوصفة.');
        this.load(rx.id);
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }

  async cancelPrescription(): Promise<void> {
    const rx = this.prescription();
    if (!rx) {
      return;
    }
    const ok = await this.confirm.confirm({
      title: 'إلغاء الوصفة',
      message: 'هل تريد إلغاء هذه الوصفة؟',
      confirmLabel: 'إلغاء الوصفة',
      variant: 'danger'
    });
    if (!ok) {
      return;
    }

    this.api.cancel(rx.id, { reason: null }).subscribe({
      next: () => {
        this.toast.success('تم إلغاء الوصفة.');
        this.load(rx.id);
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }

  canCancel(rx: PrescriptionDto): boolean {
    return rx.status === 'Draft' || rx.status === 'Issued';
  }

  trackItem(_index: number, item: PrescriptionItemDto): string {
    return item.id;
  }

  private onItemMutationSuccess(updated: PrescriptionDto): void {
    this.prescription.set(updated);
    this.itemBusy.set(false);
    this.editingItem.set(null);
    this.pickedMedication.set(null);
    this.itemForm.reset({ sortOrder: updated.items.length });
    this.toast.success('تم تحديث بنود الوصفة.');
  }

  private onItemMutationError(err: HttpErrorResponse, id: string): void {
    this.itemBusy.set(false);
    this.toast.error(this.errors.resolveMessage(err));
    if (err.status === 409) {
      this.load(id);
    }
  }

  private applyNotesState(): void {
    if (this.editable()) {
      this.notesForm.enable({ emitEvent: false });
    } else {
      this.notesForm.disable({ emitEvent: false });
    }
  }
}
