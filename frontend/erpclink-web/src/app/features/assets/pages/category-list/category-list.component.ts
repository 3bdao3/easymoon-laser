import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AssetsApiService } from '../../services/assets-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';

@Component({
  selector: 'app-asset-category-list',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, HasPermissionDirective],
  template: `
    <app-page-header title="فئات الأصول" subtitle="Asset categories" />
    <form class="feature-form" (ngSubmit)="create()" *appHasPermission="permissions.AssetsCategoriesCreate">
      <input [(ngModel)]="newName" name="name" placeholder="اسم الفئة" required />
      <button type="submit" class="btn btn-primary">إضافة</button>
    </form>
    @if (loading()) { <app-loading-spinner /> } @else {
      <ul class="feature-list">
        @for (c of items(); track c.id) {
          <li>{{ c.code }} — {{ c.name }}</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class CategoryListComponent {
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
    this.api.createCategory(this.newName.trim()).subscribe({
      next: () => {
        this.newName = '';
        this.reload();
      }
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.searchCategories().subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
