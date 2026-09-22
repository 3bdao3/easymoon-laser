import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Permissions } from '../../../../core/permissions/permissions';
import { ToastService } from '../../../../core/services/toast.service';
import { ConfirmService } from '../../../../shared/components/confirm-dialog/confirm.service';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { ReferenceApiService } from '../../../../shared/services/reference-api.service';
import { formatDateTimeUtc, formatTimeOnly } from '../../../../shared/utils/date.utils';
import { QueueListItem } from '../../models/queue.models';
import { QueueApiService } from '../../services/queue-api.service';
import { QUEUE_STATUS_OPTIONS, queueActionFlags } from '../../utils/queue-actions.util';

@Component({
  selector: 'app-queue-today',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent,
    HasPermissionDirective
  ],
  templateUrl: './queue-today.component.html',
  styleUrl: './queue-today.component.scss'
})
export class QueueTodayComponent {
  readonly permissions = Permissions;
  readonly statusOptions = QUEUE_STATUS_OPTIONS;
  readonly formatDateTimeUtc = formatDateTimeUtc;
  readonly formatTimeOnly = formatTimeOnly;
  readonly actionFlags = queueActionFlags;

  private readonly api = inject(QueueApiService);
  private readonly referenceApi = inject(ReferenceApiService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly clinicId = signal('');
  readonly doctorId = signal('');
  readonly status = signal('');
  readonly clinicOptions = signal<{ id: string; label: string }[]>([]);
  readonly entries = signal<QueueListItem[]>([]);
  readonly loading = signal(false);
  readonly busyId = signal<string | null>(null);

  constructor() {
    this.referenceApi.searchClinics({ isActive: true, page: 1, pageSize: 100 }).subscribe({
      next: (r) => this.clinicOptions.set(r.items.map((c) => ({ id: c.id, label: c.name })))
    });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api
      .getToday({
        clinicId: this.clinicId() || undefined,
        doctorId: this.doctorId() || undefined,
        status: this.status() || undefined
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (items) => this.entries.set(items),
        error: () => this.entries.set([])
      });
  }

  async runAction(
    entry: QueueListItem,
    kind: 'call' | 'start' | 'complete' | 'skip' | 'cancel'
  ): Promise<void> {
    const messages: Record<typeof kind, { title: string; message: string; variant?: 'danger' }> = {
      call: { title: 'نداء', message: `نداء ${entry.queueNumber}؟` },
      start: { title: 'بدء الخدمة', message: `بدء الخدمة لـ ${entry.queueNumber}؟` },
      complete: { title: 'إنهاء', message: `إنهاء ${entry.queueNumber}؟` },
      skip: { title: 'تخطي', message: `تخطي ${entry.queueNumber}؟`, variant: 'danger' },
      cancel: { title: 'إلغاء', message: `إلغاء ${entry.queueNumber}؟`, variant: 'danger' }
    };
    const ok = await this.confirm.confirm(messages[kind]);
    if (!ok) {
      return;
    }
    this.busyId.set(entry.id);
    const request$ =
      kind === 'call'
        ? this.api.call(entry.id)
        : kind === 'start'
          ? this.api.startService(entry.id)
          : kind === 'complete'
            ? this.api.complete(entry.id)
            : kind === 'skip'
              ? this.api.skip(entry.id)
              : this.api.cancel(entry.id);

    request$.pipe(finalize(() => this.busyId.set(null))).subscribe({
      next: () => {
        this.toast.success('تم تحديث الطابور');
        this.load();
      }
    });
  }
}
