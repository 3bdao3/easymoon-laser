import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PackagesApiService } from '../../services/packages-api.service';
import { ServicesApiService } from '../../../services/services/services-api.service';
import { HealthcarePackageDto, PackageItemDto } from '../../models/package.models';
import { HealthcareServiceListItemDto } from '../../../services/models/service.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ConfirmService } from '../../../../shared/components/confirm-dialog/confirm.service';

@Component({
  selector: 'app-package-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    LoadingSpinnerComponent,
    HasPermissionDirective
  ],
  templateUrl: './package-detail.component.html',
  styleUrls: ['./package-detail.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class PackageDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(PackagesApiService);
  private readonly servicesApi = inject(ServicesApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly pkg = signal<HealthcarePackageDto | null>(null);
  readonly activeServices = signal<HealthcareServiceListItemDto[]>([]);
  readonly savingHeader = signal(false);

  readonly headerForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: ['']
  });

  readonly addItemForm = this.fb.nonNullable.group({
    serviceId: ['', Validators.required],
    quantity: [1, [Validators.required, Validators.min(0.01)]],
    sortOrder: [1, Validators.min(1)]
  });

  private packageId = '';

  constructor() {
    this.packageId = this.route.snapshot.paramMap.get('id') ?? '';
    this.servicesApi.search({ isActive: true, pageSize: 200 }).subscribe({
      next: (r) => this.activeServices.set(r.items),
      error: () => this.activeServices.set([])
    });
    this.reload();
  }

  reload(): void {
    if (!this.packageId) {
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.api.getById(this.packageId).subscribe({
      next: (p) => {
        this.pkg.set(p);
        this.headerForm.patchValue({ name: p.name, description: p.description ?? '' });
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.toast.error(this.errors.resolveMessage(err));
      }
    });
  }

  saveHeader(): void {
    const p = this.pkg();
    if (!p || this.headerForm.invalid || this.savingHeader()) return;
    this.savingHeader.set(true);
    const raw = this.headerForm.getRawValue();
    this.api
      .update(p.id, { name: raw.name, description: raw.description || null, rowVersion: p.rowVersion })
      .subscribe({
        next: (updated) => {
          this.pkg.set(updated);
          this.savingHeader.set(false);
          this.toast.success('تم تحديث الباقة.');
        },
        error: (err: HttpErrorResponse) => {
          this.savingHeader.set(false);
          this.toast.error(this.errors.resolveMessage(err));
        }
      });
  }

  addItem(): void {
    const p = this.pkg();
    if (!p || !p.isActive || this.addItemForm.invalid) {
      this.addItemForm.markAllAsTouched();
      return;
    }
    const raw = this.addItemForm.getRawValue();
    this.api
      .addItem(p.id, {
        serviceId: raw.serviceId,
        quantity: raw.quantity,
        sortOrder: raw.sortOrder
      })
      .subscribe({
        next: (updated) => {
          this.pkg.set(updated);
          this.addItemForm.reset({ serviceId: '', quantity: 1, sortOrder: (updated.items.length || 0) + 1 });
          this.toast.success('تمت إضافة الخدمة.');
        },
        error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
      });
  }

  async removeItem(item: PackageItemDto): Promise<void> {
    const p = this.pkg();
    if (!p || !p.isActive) return;
    const ok = await this.confirm.confirm({
      title: 'إزالة خدمة',
      message: `إزالة «${item.serviceName}» من الباقة؟`,
      confirmLabel: 'إزالة',
      variant: 'danger'
    });
    if (!ok) return;
    this.api.removeItem(p.id, item.id, p.rowVersion).subscribe({
      next: (updated) => {
        this.pkg.set(updated);
        this.toast.success('تمت الإزالة.');
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }

  statusLabel(p: HealthcarePackageDto): string {
    return p.isActive ? 'Active' : 'Inactive';
  }
}
