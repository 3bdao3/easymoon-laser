import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AssetsApiService } from '../../services/assets-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';

@Component({
  selector: 'app-asset-location-list',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, HasPermissionDirective],
  template: `
    <app-page-header title="مواقع الأصول" subtitle="Asset locations" />
    <form class="feature-form" (ngSubmit)="create()" *appHasPermission="permissions.AssetsLocationsCreate">
      <input [(ngModel)]="newName" name="name" placeholder="اسم الموقع" required />
      <button type="submit" class="btn btn-primary">إضافة</button>
    </form>
    @if (loading()) { <app-loading-spinner /> } @else {
      <ul class="feature-list">
        @for (l of items(); track l.id) {
          <li>{{ l.code }} — {{ l.name }}</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class LocationListComponent {
  private readonly api = inject(AssetsApiService);
  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly items = signal<{ id: string; code: string; name: string }[]>([]);
  newName = '';

  constructor() {
    this.reload();
  }

  create(): void {
    if (!this.newName.trim()) return;
    this.api.createLocation(this.newName.trim()).subscribe({
      next: () => {
        this.newName = '';
        this.reload();
      }
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.searchLocations().subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
