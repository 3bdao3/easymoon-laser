import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Permissions } from '../../../core/permissions/permissions';
import { LaserServicesApi } from '../../laser-clinic/services/laser-services-api.service';
import { LaserServiceDto } from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-laser-services-list',
  standalone: true,
  imports: [
    RouterLink,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    HasPermissionDirective
  ],
  templateUrl: './laser-services-list.component.html',
  styleUrl: './laser-services-list.component.scss'
})
export class LaserServicesListComponent implements OnInit {
  private readonly api = inject(LaserServicesApi);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly items = signal<LaserServiceDto[]>([]);

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
}
