import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LaserServicesApi } from '../../laser-clinic/services/laser-services-api.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-laser-service-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './laser-service-form.component.html'
})
export class LaserServiceFormComponent implements OnInit {
  private readonly api = inject(LaserServicesApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly serviceId = signal<string | null>(null);

  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    minDurationMinutes: new FormControl(30, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1)]
    }),
    maxDurationMinutes: new FormControl(45, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1)]
    }),
    displayOrder: new FormControl(0, { nonNullable: true }),
    notes: new FormControl('', { nonNullable: true }),
    isActive: new FormControl(true, { nonNullable: true })
  });

  get isEdit(): boolean {
    return !!this.serviceId();
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }
    this.serviceId.set(id);
    this.loading.set(true);
    this.api.getById(id).subscribe({
      next: (s) => {
        this.form.patchValue({
          name: s.name,
          minDurationMinutes: s.minDurationMinutes,
          maxDurationMinutes: s.maxDurationMinutes,
          displayOrder: s.displayOrder,
          notes: s.notes ?? '',
          isActive: s.isActive
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    if (raw.maxDurationMinutes < raw.minDurationMinutes) {
      this.toast.error('الحد الأقصى يجب أن يكون أكبر من أو يساوي الحد الأدنى');
      return;
    }
    this.saving.set(true);
    const id = this.serviceId();
    if (id) {
      this.api
        .update(id, {
          name: raw.name.trim(),
          minDurationMinutes: raw.minDurationMinutes,
          maxDurationMinutes: raw.maxDurationMinutes,
          displayOrder: raw.displayOrder,
          notes: raw.notes.trim() || null,
          isActive: raw.isActive
        })
        .subscribe({
          next: () => {
            this.toast.success('تم تحديث الخدمة');
            this.saving.set(false);
            void this.router.navigate(['/app/laser-services']);
          },
          error: () => this.saving.set(false)
        });
    } else {
      this.api
        .create({
          name: raw.name.trim(),
          minDurationMinutes: raw.minDurationMinutes,
          maxDurationMinutes: raw.maxDurationMinutes,
          displayOrder: raw.displayOrder,
          notes: raw.notes.trim() || null
        })
        .subscribe({
          next: () => {
            this.toast.success('تم إنشاء الخدمة');
            this.saving.set(false);
            void this.router.navigate(['/app/laser-services']);
          },
          error: () => this.saving.set(false)
        });
    }
  }
}
