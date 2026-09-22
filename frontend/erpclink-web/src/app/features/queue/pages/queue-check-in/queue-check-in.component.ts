import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastService } from '../../../../core/services/toast.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { QueueApiService } from '../../services/queue-api.service';

@Component({
  selector: 'app-queue-check-in',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './queue-check-in.component.html',
  styleUrl: './queue-check-in.component.scss'
})
export class QueueCheckInComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly api = inject(QueueApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);

  readonly priorityOptions = [
    { value: '', label: 'عادي (افتراضي)' },
    { value: 'Normal', label: 'Normal' },
    { value: 'Urgent', label: 'Urgent' }
  ];

  readonly form = this.fb.group({
    appointmentId: ['', Validators.required],
    priority: ['']
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    this.submitting.set(true);
    this.api
      .checkIn({
        appointmentId: raw.appointmentId.trim(),
        priority: raw.priority.trim() ? raw.priority : null
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (entry) => {
          this.toast.success(`تم التسجيل — ${entry.queueNumber}`);
          void this.router.navigate(['/app/queue']);
        }
      });
  }
}
