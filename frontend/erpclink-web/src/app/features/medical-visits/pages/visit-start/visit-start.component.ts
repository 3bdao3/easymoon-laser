import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MedicalVisitsApiService } from '../../services/medical-visits-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

@Component({
  selector: 'app-visit-start',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './visit-start.component.html',
  styleUrls: ['./visit-start.component.scss', '../../_feature-layout.scss']
})
export class VisitStartComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MedicalVisitsApiService);
  private readonly router = inject(Router);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly submitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    appointmentId: ['', [Validators.required, Validators.pattern(UUID_PATTERN)]],
    queueEntryId: ['', [Validators.pattern(UUID_PATTERN)]]
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.submitting.set(true);
    this.api
      .start({
        appointmentId: raw.appointmentId,
        queueEntryId: raw.queueEntryId || null
      })
      .subscribe({
        next: (visit) => {
          this.submitting.set(false);
          this.toast.success('تم بدء الزيارة بنجاح.');
          void this.router.navigate(['/app/medical-visits', visit.id]);
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          this.toast.error(this.errors.resolveMessage(err));
        }
      });
  }
}
