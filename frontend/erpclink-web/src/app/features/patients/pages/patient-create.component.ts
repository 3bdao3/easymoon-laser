import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PatientFormComponent } from '../components/patient-form.component';
import { PatientsApi } from '../services/patients.api';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmService } from '../../../shared/components/confirm-dialog/confirm.service';
import { PatientListItemDto } from '../models/patient.models';
import { createPatientForm } from '../utils/patient-form.factory';
import { formToRegisterRequest } from '../utils/patient-payload.util';

@Component({
  selector: 'app-patient-create',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, PatientFormComponent],
  templateUrl: './patient-create.component.html'
})
export class PatientCreateComponent {
  private readonly api = inject(PatientsApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly form = createPatientForm();
  readonly submitting = signal(false);
  readonly allowDuplicate = new FormControl(false, { nonNullable: true });
  readonly duplicates = signal<PatientListItemDto[]>([]);

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }

    this.submitting.set(true);
    const body = formToRegisterRequest(this.form, this.allowDuplicate.value);

    this.api.register(body).subscribe({
      next: (result) => {
        this.toast.success('تم تسجيل المريض');
        void this.router.navigate(['/app/patients', result.patient.id]);
      },
      error: (err: unknown) => {
        this.submitting.set(false);
        if (err instanceof HttpErrorResponse && err.status === 409) {
          const code =
            typeof err.error === 'object' && err.error && 'code' in err.error
              ? String((err.error as { code?: string }).code)
              : '';
          if (code === 'patients.possible_duplicate') {
            this.loadPossibleDuplicates(body.phoneNumber);
          }
        }
      }
    });
  }

  private loadPossibleDuplicates(phoneNumber: string): void {
    this.api.search({ phoneNumber, pageSize: 10 }).subscribe({
      next: (page) => {
        this.duplicates.set(page.items);
        this.toast.warning('تم العثور على مرضى مشابهين — راجع القائمة أو أكّد المتابعة.');
      }
    });
  }

  async confirmDespiteDuplicates(): Promise<void> {
    const ok = await this.confirm.confirm({
      title: 'متابعة التسجيل',
      message: 'هل تريد تسجيل المريض رغم وجود سجلات مشابهة؟',
      confirmLabel: 'متابعة'
    });
    if (!ok) {
      return;
    }
    this.allowDuplicate.setValue(true);
    this.submit();
  }
}
