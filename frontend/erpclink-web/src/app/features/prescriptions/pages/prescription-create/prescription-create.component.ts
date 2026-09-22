import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PrescriptionsApiService } from '../../services/prescriptions-api.service';
import { PrescriptionItemInput } from '../../models/prescription.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { MedicationPickerComponent } from '../../components/medication-picker/medication-picker.component';
import { MedicationDto } from '../../../medications/models/medication.models';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

interface DraftLine extends PrescriptionItemInput {
  medicationNameSnapshot: string;
}

@Component({
  selector: 'app-prescription-create',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    MedicationPickerComponent
  ],
  templateUrl: './prescription-create.component.html',
  styleUrls: [
    './prescription-create.component.scss',
    '../../../medical-visits/_feature-layout.scss',
    '../../_feature-layout.scss'
  ]
})
export class PrescriptionCreateComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(PrescriptionsApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly submitting = signal(false);
  readonly lines = signal<DraftLine[]>([]);
  readonly pickedMedication = signal<MedicationDto | null>(null);

  readonly headerForm = this.fb.nonNullable.group({
    medicalVisitId: [
      this.route.snapshot.queryParamMap.get('medicalVisitId') ?? '',
      [Validators.required, Validators.pattern(UUID_PATTERN)]
    ],
    notes: ['']
  });

  readonly itemForm = this.fb.nonNullable.group({
    dosage: ['', Validators.required],
    frequency: ['', Validators.required],
    duration: [''],
    route: [''],
    instructions: [''],
    quantity: [''],
    notes: ['']
  });

  onMedicationPicked(med: MedicationDto): void {
    this.pickedMedication.set(med);
  }

  addLine(): void {
    const med = this.pickedMedication();
    if (!med || this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      if (!med) {
        this.toast.warning('اختر دواءً أولاً.');
      }
      return;
    }

    const raw = this.itemForm.getRawValue();
    const qty = raw.quantity.trim() ? Number(raw.quantity) : null;
    const line: DraftLine = {
      medicationId: med.id,
      medicationNameSnapshot: med.name,
      dosage: raw.dosage,
      frequency: raw.frequency,
      duration: raw.duration || null,
      route: raw.route || null,
      instructions: raw.instructions || null,
      quantity: qty !== null && !Number.isNaN(qty) ? qty : null,
      notes: raw.notes || null,
      sortOrder: this.lines().length
    };
    this.lines.update((list) => [...list, line]);
    this.pickedMedication.set(null);
    this.itemForm.reset();
  }

  removeLine(index: number): void {
    this.lines.update((list) => list.filter((_, i) => i !== index));
  }

  submit(): void {
    if (this.headerForm.invalid || this.submitting()) {
      this.headerForm.markAllAsTouched();
      return;
    }

    const header = this.headerForm.getRawValue();
    const items: PrescriptionItemInput[] = this.lines().map((line, index) => ({
      medicationId: line.medicationId,
      dosage: line.dosage,
      frequency: line.frequency,
      duration: line.duration,
      route: line.route,
      instructions: line.instructions,
      quantity: line.quantity,
      notes: line.notes,
      sortOrder: index
    }));

    this.submitting.set(true);
    this.api
      .create({
        medicalVisitId: header.medicalVisitId,
        notes: header.notes || null,
        items: items.length > 0 ? items : null
      })
      .subscribe({
        next: (rx) => {
          this.submitting.set(false);
          this.toast.success('تم إنشاء الوصفة.');
          void this.router.navigate(['/app/prescriptions', rx.id]);
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          this.toast.error(this.errors.resolveMessage(err));
        }
      });
  }
}
