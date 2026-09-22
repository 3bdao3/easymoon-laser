import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AssetsApiService } from '../../services/assets-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';

@Component({
  selector: 'app-asset-list',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, RouterLink, FormsModule, HasPermissionDirective],
  template: `
    <app-page-header title="الأصول الثابتة" subtitle="Fixed assets">
      <a actions routerLink="/app/assets/categories" class="btn btn--ghost">الفئات</a>
      <a actions routerLink="/app/assets/locations" class="btn btn--ghost">المواقع</a>
    </app-page-header>

    <form class="feature-form" (ngSubmit)="create()" *appHasPermission="permissions.AssetsCreate">
      <input [(ngModel)]="form.name" name="name" placeholder="اسم الأصل" required />
      <select [(ngModel)]="form.categoryId" name="categoryId" required>
        <option value="">الفئة</option>
        @for (c of categories(); track c.id) {
          <option [value]="c.id">{{ c.name }}</option>
        }
      </select>
      <select [(ngModel)]="form.locationId" name="locationId" required>
        <option value="">الموقع</option>
        @for (l of locations(); track l.id) {
          <option [value]="l.id">{{ l.name }}</option>
        }
      </select>
      <button type="submit" class="btn btn-primary">تسجيل أصل</button>
    </form>

    @if (loading()) { <app-loading-spinner /> } @else {
      <ul class="feature-list">
        @for (a of items(); track a.id) {
          <li>
            <a [routerLink]="['/app/assets', a.id]">{{ a.assetNumber }} — {{ a.name }} ({{ a.status }})</a>
          </li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class AssetListComponent {
  private readonly api = inject(AssetsApiService);
  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly items = signal<{ id: string; assetNumber: string; name: string; status: string }[]>([]);
  readonly categories = signal<{ id: string; name: string }[]>([]);
  readonly locations = signal<{ id: string; name: string }[]>([]);
  form = { name: '', categoryId: '', locationId: '' };

  constructor() {
    this.reload();
    this.api.searchCategories().subscribe({ next: (p) => this.categories.set(p.items) });
    this.api.searchLocations().subscribe({ next: (p) => this.locations.set(p.items) });
  }

  create(): void {
    if (!this.form.name.trim() || !this.form.categoryId || !this.form.locationId) return;
    const today = new Date().toISOString().slice(0, 10);
    this.api
      .createAsset({
        name: this.form.name.trim(),
        assetCategoryId: this.form.categoryId,
        assetLocationId: this.form.locationId,
        purchaseDate: today
      })
      .subscribe({
        next: () => {
          this.form = { name: '', categoryId: '', locationId: '' };
          this.reload();
        }
      });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.searchAssets().subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
