import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Permissions } from '../../../core/permissions/permissions';
import { LaserOffersApi } from '../../laser-clinic/services/laser-offers-api.service';
import { LaserOfferDto, formatDateAr } from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-offers-list',
  standalone: true,
  imports: [
    RouterLink,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    HasPermissionDirective
  ],
  templateUrl: './offers-list.component.html',
  styleUrl: './offers-list.component.scss'
})
export class OffersListComponent implements OnInit {
  private readonly api = inject(LaserOffersApi);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly items = signal<LaserOfferDto[]>([]);
  readonly formatDateAr = formatDateAr;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.list(false).subscribe({
      next: (rows) => {
        this.items.set(rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  validityLabel(row: LaserOfferDto): string {
    if (!row.validFrom && !row.validTo) {
      return 'مفتوح بدون تاريخ انتهاء';
    }
    const from = row.validFrom ? formatDateAr(row.validFrom) : '—';
    const to = row.validTo ? formatDateAr(row.validTo) : '—';
    return `${from} – ${to}`;
  }
}
