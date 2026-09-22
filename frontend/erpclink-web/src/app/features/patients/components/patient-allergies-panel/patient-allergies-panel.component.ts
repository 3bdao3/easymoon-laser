import { Component, input, output, inject, signal, effect } from '@angular/core';
import { NgClass } from '@angular/common';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { ConfirmService } from '../../../../shared/components/confirm-dialog/confirm.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Permissions } from '../../../../core/permissions/permissions';
import { trimToNull } from '../../../../shared/utils/form.util';
import { PatientsApi } from '../../services/patients.api';
import {
  ALLERGY_SEVERITY_LABELS,
  AllergyDto,
  AllergySeverity
} from '../../models/patient.models';

@Component({
  selector: 'app-patient-allergies-panel',
  standalone: true,
  imports: [
    NgClass,
    ReactiveFormsModule,
    StatusBadgeComponent,
    EmptyStateComponent,
    HasPermissionDirective
  ],
  templateUrl: './patient-allergies-panel.component.html',
  styleUrl: './patient-allergies-panel.component.scss'
})
export class PatientAllergiesPanelComponent {
  private readonly api = inject(PatientsApi);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly patientId = input.required<string>();
  readonly patientActive = input(true);
  readonly initialAllergies = input<AllergyDto[]>([]);
  readonly compact = input(false);

  readonly allergiesChange = output<AllergyDto[]>();

  readonly permissions = Permissions;
  readonly severityLabels = ALLERGY_SEVERITY_LABELS;
  readonly allergies = signal<AllergyDto[]>([]);
  readonly allergySaving = signal(false);
  readonly editingAllergyId = signal<string | null>(null);

  readonly severityOptions = (
    Object.entries(ALLERGY_SEVERITY_LABELS) as [string, string][]
  ).map(([value, label]) => ({
    value: Number(value) as AllergySeverity,
    label
  }));

  readonly allergyForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    reaction: new FormControl('', { nonNullable: true }),
    severity: new FormControl(AllergySeverity.Unknown, { nonNullable: true }),
    notes: new FormControl('', { nonNullable: true })
  });

  constructor() {
    effect(() => {
      this.allergies.set(this.initialAllergies());
    });
    effect(() => {
      if (this.patientActive()) {
        this.allergyForm.enable({ emitEvent: false });
      } else {
        this.allergyForm.disable({ emitEvent: false });
      }
    });
  }

  activeAllergies(): AllergyDto[] {
    return this.allergies().filter((a) => a.isActive);
  }

  refresh(): void {
    this.api.getAllergies(this.patientId()).subscribe({
      next: (list) => {
        this.allergies.set(list);
        this.allergiesChange.emit(list);
      }
    });
  }

  startEditAllergy(allergy: AllergyDto): void {
    this.editingAllergyId.set(allergy.id);
    this.allergyForm.setValue({
      name: allergy.name,
      reaction: allergy.reaction ?? '',
      severity: allergy.severity,
      notes: allergy.notes ?? ''
    });
  }

  cancelAllergyEdit(): void {
    this.editingAllergyId.set(null);
    this.allergyForm.reset({
      name: '',
      reaction: '',
      severity: AllergySeverity.Unknown,
      notes: ''
    });
  }

  saveAllergy(): void {
    this.allergyForm.markAllAsTouched();
    if (this.allergyForm.invalid) {
      return;
    }
    if (!this.patientActive()) {
      this.toast.error('لا يمكن تعديل حساسيات مريض موقوف');
      return;
    }
    const v = this.allergyForm.getRawValue();
    const severity = Number(v.severity) as AllergySeverity;
    const body = {
      name: v.name.trim(),
      reaction: trimToNull(v.reaction),
      severity: Number.isFinite(severity) ? severity : AllergySeverity.Unknown,
      notes: trimToNull(v.notes)
    };
    this.allergySaving.set(true);
    const editId = this.editingAllergyId();
    const req = editId
      ? this.api.updateAllergy(this.patientId(), editId, body)
      : this.api.addAllergy(this.patientId(), body);

    req.subscribe({
      next: () => {
        this.toast.success(editId ? 'تم تحديث الحساسية' : 'تمت إضافة الحساسية');
        this.cancelAllergyEdit();
        this.allergySaving.set(false);
        this.refresh();
      },
      error: () => this.allergySaving.set(false)
    });
  }

  async deactivateAllergy(allergy: AllergyDto): Promise<void> {
    const ok = await this.confirm.confirm({
      title: 'إيقاف حساسية',
      message: `إيقاف "${allergy.name}"؟`,
      variant: 'danger'
    });
    if (!ok) {
      return;
    }
    this.api.deactivateAllergy(this.patientId(), allergy.id).subscribe({
      next: () => {
        this.toast.success('تم إيقاف الحساسية');
        this.refresh();
      }
    });
  }

  severityClass(severity: AllergySeverity): string {
    if (severity === AllergySeverity.LifeThreatening || severity === AllergySeverity.Severe) {
      return 'allergy-chip--critical';
    }
    if (severity === AllergySeverity.Moderate) {
      return 'allergy-chip--warn';
    }
    return 'allergy-chip--neutral';
  }
}
