import { Component, inject, signal, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../core/permissions/permissions';
import { ConfirmService } from '../../../shared/components/confirm-dialog/confirm.service';
import { ToastService } from '../../../core/services/toast.service';
import { enrichPagedResult, PagedResult } from '../../../shared/models/api.models';
import { PatientsApi } from '../services/patients.api';
import { PatientListItemDto } from '../models/patient.models';

@Component({
  selector: 'app-patient-list',
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
  templateUrl: './patient-list.component.html'
})
export class PatientListComponent implements OnInit {
  private readonly api = inject(PatientsApi);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly page = signal<PagedResult<PatientListItemDto> | null>(null);

  readonly filters = new FormGroup({
    query: new FormControl('', { nonNullable: true }),
    isActive: new FormControl<'all' | 'true' | 'false'>('all', { nonNullable: true })
  });

  private currentPage = 1;
  readonly pageSize = 20;

  ngOnInit(): void {
    this.load();
    this.filters.controls.query.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged())
      .subscribe(() => {
        this.currentPage = 1;
        this.load();
      });
    this.filters.controls.isActive.valueChanges.subscribe(() => {
      this.currentPage = 1;
      this.load();
    });
  }

  load(): void {
    this.loading.set(true);
    const isActiveRaw = this.filters.controls.isActive.value;
    const isActive =
      isActiveRaw === 'all' ? undefined : isActiveRaw === 'true' ? true : false;

    this.api
      .search({
        query: this.filters.controls.query.value.trim() || undefined,
        isActive,
        page: this.currentPage,
        pageSize: this.pageSize
      })
      .subscribe({
        next: (result) => {
          this.page.set(enrichPagedResult(result));
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  goPage(delta: number): void {
    const p = this.page();
    if (!p) {
      return;
    }
    const next = p.page + delta;
    if (next < 1 || next > p.totalPages) {
      return;
    }
    this.currentPage = next;
    this.load();
  }

  async toggleActive(item: PatientListItemDto): Promise<void> {
    const activate = !item.isActive;
    const ok = await this.confirm.confirm({
      title: activate ? 'تفعيل المريض' : 'إيقاف المريض',
      message: activate
        ? `تفعيل ${item.fullName}؟`
        : `إيقاف ${item.fullName}؟ لن يُحذف السجل.`,
      variant: activate ? 'default' : 'danger',
      confirmLabel: activate ? 'تفعيل' : 'إيقاف'
    });
    if (!ok) {
      return;
    }

    const req = activate ? this.api.activate(item.id) : this.api.deactivate(item.id);
    req.subscribe({
      next: () => {
        this.toast.success(activate ? 'تم التفعيل' : 'تم الإيقاف');
        this.load();
      }
    });
  }
}
