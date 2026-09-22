import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ProcurementApiService } from '../../services/procurement-api.service';
import { PurchaseOrderDto } from '../../models/purchase-order.models';
import {
  canApprovePurchaseOrder,
  canCancelPurchaseOrder,
  canSubmitPurchaseOrder
} from '../../purchase-orders/purchase-order-actions';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-purchase-order-detail',
  standalone: true,
  imports: [DecimalPipe, PageHeaderComponent, LoadingSpinnerComponent, HasPermissionDirective],
  templateUrl: './purchase-order-detail.component.html',
  styleUrls: ['./purchase-order-detail.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class PurchaseOrderDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ProcurementApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly po = signal<PurchaseOrderDto | null>(null);

  readonly canSubmit = computed(() => {
    const p = this.po();
    return p ? canSubmitPurchaseOrder(p.status, p.lines.length) : false;
  });
  readonly canApprove = computed(() => {
    const p = this.po();
    return p ? canApprovePurchaseOrder(p.status) : false;
  });
  readonly canCancel = computed(() => {
    const p = this.po();
    return p ? canCancelPurchaseOrder(p.status) : false;
  });

  constructor() {
    this.reload(this.route.snapshot.paramMap.get('id')!);
  }

  reload(id: string): void {
    this.loading.set(true);
    this.api.getPurchaseOrder(id).subscribe({
      next: (p) => {
        this.po.set(p);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.toast.error(this.errors.resolveMessage(err));
      }
    });
  }

  submit(): void {
    const p = this.po();
    if (!p) return;
    this.api.submitPurchaseOrder(p.id, p.rowVersion).subscribe({
      next: (updated) => {
        this.po.set(updated);
        this.toast.success('تم إرسال الأمر');
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }

  approve(): void {
    const p = this.po();
    if (!p) return;
    this.api.approvePurchaseOrder(p.id, p.rowVersion).subscribe({
      next: (updated) => {
        this.po.set(updated);
        this.toast.success('تم اعتماد الأمر');
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }

  cancel(): void {
    const p = this.po();
    if (!p) return;
    this.api.cancelPurchaseOrder(p.id, 'Cancelled from UI', p.rowVersion).subscribe({
      next: (updated) => {
        this.po.set(updated);
        this.toast.success('تم إلغاء الأمر');
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }
}
