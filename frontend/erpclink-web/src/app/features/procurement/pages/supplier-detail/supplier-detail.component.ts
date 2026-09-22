import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ProcurementApiService } from '../../services/procurement-api.service';
import { SupplierDto } from '../../models/supplier.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-supplier-detail',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, HasPermissionDirective],
  templateUrl: './supplier-detail.component.html',
  styleUrls: ['./supplier-detail.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class SupplierDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ProcurementApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly supplier = signal<SupplierDto | null>(null);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.api.getSupplier(id).subscribe({
      next: (s) => {
        this.supplier.set(s);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.toast.error(this.errors.resolveMessage(err));
      }
    });
  }

  toggleActive(): void {
    const s = this.supplier();
    if (!s) return;
    const call = s.isActive
      ? this.api.deactivateSupplier(s.id, s.rowVersion)
      : this.api.activateSupplier(s.id, s.rowVersion);
    call.subscribe({
      next: () => {
        this.toast.success(s.isActive ? 'تم إيقاف المورد' : 'تم تفعيل المورد');
        this.api.getSupplier(s.id).subscribe((updated) => this.supplier.set(updated));
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }
}
