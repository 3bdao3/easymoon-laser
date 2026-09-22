import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ServicesApiService } from '../../services/services-api.service';
import { ServiceCategoryDto } from '../../models/service.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-service-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './service-form.component.html',
  styleUrls: ['./service-form.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class ServiceFormComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ServicesApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly serviceId = this.route.snapshot.paramMap.get('id');
  readonly isEdit = !!this.serviceId;
  readonly loading = signal(this.isEdit);
  readonly submitting = signal(false);
  readonly categories = signal<ServiceCategoryDto[]>([]);
  private rowVersion: string | null = null;

  readonly form = this.fb.nonNullable.group({
    serviceCode: [{ value: '', disabled: this.isEdit }, [Validators.required, Validators.maxLength(64)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    categoryId: [''],
    defaultPrice: [0, [Validators.required, Validators.min(0)]],
    currencyCode: ['EGP', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    durationMinutes: [null as number | null]
  });

  constructor() {
    this.api.searchCategories().subscribe({
      next: (r) => this.categories.set(r.items),
      error: () => this.categories.set([])
    });

    if (this.serviceId) {
      this.api.getById(this.serviceId).subscribe({
        next: (svc) => {
          this.rowVersion = svc.rowVersion;
          this.form.patchValue({
            serviceCode: svc.serviceCode,
            name: svc.name,
            description: svc.description ?? '',
            categoryId: svc.categoryId ?? '',
            defaultPrice: svc.defaultPrice,
            currencyCode: svc.currencyCode,
            durationMinutes: svc.durationMinutes
          });
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.toast.error(this.errors.resolveMessage(err));
        }
      });
    }
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    const raw = this.form.getRawValue();
    const categoryId = raw.categoryId ? raw.categoryId : null;

    if (this.isEdit && this.serviceId) {
      this.api
        .update(this.serviceId, {
          name: raw.name,
          description: raw.description || null,
          categoryId,
          defaultPrice: raw.defaultPrice,
          currencyCode: raw.currencyCode,
          durationMinutes: raw.durationMinutes,
          rowVersion: this.rowVersion
        })
        .subscribe({
          next: () => {
            this.submitting.set(false);
            this.toast.success('تم تحديث الخدمة.');
            void this.router.navigate(['/app/services', this.serviceId]);
          },
          error: (err: HttpErrorResponse) => {
            this.submitting.set(false);
            this.toast.error(this.errors.resolveMessage(err));
          }
        });
    } else {
      this.api
        .create({
          serviceCode: raw.serviceCode,
          name: raw.name,
          description: raw.description || null,
          categoryId,
          defaultPrice: raw.defaultPrice,
          currencyCode: raw.currencyCode.toUpperCase(),
          durationMinutes: raw.durationMinutes
        })
        .subscribe({
          next: (svc) => {
            this.submitting.set(false);
            this.toast.success('تم إنشاء الخدمة.');
            void this.router.navigate(['/app/services', svc.id]);
          },
          error: (err: HttpErrorResponse) => {
            this.submitting.set(false);
            this.toast.error(this.errors.resolveMessage(err));
          }
        });
    }
  }
}
