import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ServicesApiService } from '../../services/services-api.service';
import { HealthcareServiceDto } from '../../models/service.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-service-detail',
  standalone: true,
  imports: [
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    LoadingSpinnerComponent,
    HasPermissionDirective
  ],
  templateUrl: './service-detail.component.html',
  styleUrls: ['./service-detail.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class ServiceDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ServicesApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly service = signal<HealthcareServiceDto | null>(null);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }
    this.api.getById(id).subscribe({
      next: (svc) => {
        this.service.set(svc);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.toast.error(this.errors.resolveMessage(err));
      }
    });
  }

  statusLabel(svc: HealthcareServiceDto): string {
    return svc.isActive ? 'Active' : 'Inactive';
  }
}
