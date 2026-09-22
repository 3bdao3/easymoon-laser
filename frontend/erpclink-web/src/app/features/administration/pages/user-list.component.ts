import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../core/permissions/permissions';
import { ConfirmService } from '../../../shared/components/confirm-dialog/confirm.service';
import { ToastService } from '../../../core/services/toast.service';
import { UsersApi } from '../services/users.api';
import { UserDto } from '../models/user.models';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    RouterLink,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    StatusBadgeComponent,
    HasPermissionDirective
  ],
  templateUrl: './user-list.component.html'
})
export class UserListComponent implements OnInit {
  private readonly api = inject(UsersApi);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly users = signal<UserDto[]>([]);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.list().subscribe({
      next: (list) => {
        this.users.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  async toggleActive(user: UserDto): Promise<void> {
    const activate = !user.isActive;
    const ok = await this.confirm.confirm({
      title: activate ? 'تفعيل المستخدم' : 'إيقاف المستخدم',
      message: `${activate ? 'تفعيل' : 'إيقاف'} ${user.fullName}؟`,
      variant: activate ? 'default' : 'danger'
    });
    if (!ok) return;
    const req = activate ? this.api.activate(user.id) : this.api.deactivate(user.id);
    req.subscribe({
      next: () => {
        this.toast.success(activate ? 'تم التفعيل' : 'تم الإيقاف');
        this.load();
      }
    });
  }
}
