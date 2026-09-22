import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../core/permissions/permissions';
import { ConfirmService } from '../../../shared/components/confirm-dialog/confirm.service';
import { ToastService } from '../../../core/services/toast.service';
import { SpecialtiesApi } from '../services/specialties.api';
import { SpecialtyDto } from '../models/specialty.models';

@Component({
  selector: 'app-specialty-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    StatusBadgeComponent,
    HasPermissionDirective
  ],
  templateUrl: './specialty-list.component.html'
})
export class SpecialtyListComponent implements OnInit {
  private readonly api = inject(SpecialtiesApi);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly items = signal<SpecialtyDto[]>([]);

  readonly filters = new FormGroup({
    isActive: new FormControl<'all' | 'true' | 'false'>('all', { nonNullable: true })
  });

  ngOnInit(): void {
    this.load();
    this.filters.controls.isActive.valueChanges.subscribe(() => this.load());
  }

  load(): void {
    this.loading.set(true);
    const raw = this.filters.controls.isActive.value;
    const isActive = raw === 'all' ? undefined : raw === 'true';
    this.api.list(isActive).subscribe({
      next: (list) => {
        this.items.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  async toggleActive(item: SpecialtyDto): Promise<void> {
    const activate = !item.isActive;
    const ok = await this.confirm.confirm({
      title: activate ? 'تفعيل التخصص' : 'إيقاف التخصص',
      message: `${activate ? 'تفعيل' : 'إيقاف'} ${item.name}؟`,
      variant: activate ? 'default' : 'danger'
    });
    if (!ok) return;
    const req = activate ? this.api.activate(item.id) : this.api.deactivate(item.id);
    req.subscribe({
      next: () => {
        this.toast.success(activate ? 'تم التفعيل' : 'تم الإيقاف');
        this.load();
      }
    });
  }
}
