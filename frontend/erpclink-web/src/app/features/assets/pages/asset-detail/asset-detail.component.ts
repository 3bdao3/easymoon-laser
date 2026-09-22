import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  canChangeLocation,
  canCompleteMaintenance,
  canRetire,
  canStartMaintenance,
  type AssetStatus
} from '../../assets/asset.utils';
import { AssetsApiService, type AssetDto, type AssetHistoryEntryDto } from '../../services/assets-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';

@Component({
  selector: 'app-asset-detail',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, HasPermissionDirective],
  template: `
    <app-page-header [title]="asset()?.name ?? 'تفاصيل الأصل'" subtitle="Asset detail" />
    @if (loading()) { <app-loading-spinner /> } @else {
      @if (asset(); as a) {
      <p>{{ a.assetNumber }} — {{ a.status }}</p>
      @if (a.purchaseDate) { <p>تاريخ الشراء: {{ a.purchaseDate }}</p> }
      @if (a.warrantyStartDate || a.warrantyEndDate) {
        <p>الضمان: {{ a.warrantyStartDate ?? '—' }} → {{ a.warrantyEndDate ?? '—' }}</p>
      }

      <div class="action-row">
        <button *appHasPermission="permissions.AssetsMaintenance" type="button" class="btn btn-primary"
          [disabled]="!showStartMaintenance()" (click)="startMaintenance()">بدء الصيانة</button>
        <button *appHasPermission="permissions.AssetsMaintenance" type="button" class="btn"
          [disabled]="!showCompleteMaintenance()" (click)="completeMaintenance()">إنهاء الصيانة</button>
        <button *appHasPermission="permissions.AssetsRetire" type="button" class="btn btn-danger"
          [disabled]="!showRetire()" (click)="retire()">إيقاف/تصفية</button>
      </div>

      <div *appHasPermission="permissions.AssetsAssign" class="feature-form">
        <select [(ngModel)]="targetLocationId" name="targetLocationId" [disabled]="!showChangeLocation()">
          <option value="">موقع جديد</option>
          @for (l of locations(); track l.id) {
            <option [value]="l.id">{{ l.name }}</option>
          }
        </select>
        <button type="button" class="btn" [disabled]="!showChangeLocation() || !targetLocationId" (click)="changeLocation()">تغيير الموقع</button>
      </div>

      <h3 *appHasPermission="permissions.AssetsHistoryView">السجل</h3>
      <ul class="feature-list" *appHasPermission="permissions.AssetsHistoryView">
        @for (h of history(); track h.id) {
          <li>{{ h.eventType }}: {{ h.summary }}</li>
        }
      </ul>
      }
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class AssetDetailComponent {
  private readonly api = inject(AssetsApiService);
  private readonly route = inject(ActivatedRoute);
  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly asset = signal<AssetDto | null>(null);
  readonly history = signal<AssetHistoryEntryDto[]>([]);
  readonly locations = signal<{ id: string; name: string }[]>([]);
  targetLocationId = '';

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    this.api.searchLocations().subscribe({ next: (p) => this.locations.set(p.items) });
    if (!id) {
      this.loading.set(false);
      return;
    }
    this.reload(id);
  }

  startMaintenance(): void {
    const a = this.asset();
    if (!a) return;
    this.api.startMaintenance(a.id, null, a.rowVersion).subscribe({ next: (x) => this.afterAction(x) });
  }

  completeMaintenance(): void {
    const a = this.asset();
    if (!a) return;
    this.api.completeMaintenance(a.id, a.rowVersion).subscribe({ next: (x) => this.afterAction(x) });
  }

  retire(): void {
    const a = this.asset();
    if (!a) return;
    const reason = prompt('سبب الإيقاف/التصفية');
    if (!reason?.trim()) return;
    this.api.retire(a.id, reason.trim(), a.rowVersion).subscribe({ next: (x) => this.afterAction(x) });
  }

  changeLocation(): void {
    const a = this.asset();
    if (!a || !this.targetLocationId) return;
    this.api.changeLocation(a.id, this.targetLocationId, a.rowVersion).subscribe({ next: (x) => this.afterAction(x) });
  }

  private afterAction(a: AssetDto): void {
    this.asset.set(a);
    this.api.getHistory(a.id).subscribe({ next: (h) => this.history.set(h) });
  }

  private reload(id: string): void {
    this.api.getAsset(id).subscribe({
      next: (a) => {
        this.asset.set(a);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
    this.api.getHistory(id).subscribe({
      next: (h) => this.history.set(h),
      error: () => this.history.set([])
    });
  }

  private status(): AssetStatus | null {
    const s = this.asset()?.status;
    if (s === 'Active' || s === 'UnderMaintenance' || s === 'Retired') return s;
    return null;
  }

  showStartMaintenance(): boolean {
    const s = this.status();
    return s ? canStartMaintenance(s) : false;
  }

  showCompleteMaintenance(): boolean {
    const s = this.status();
    return s ? canCompleteMaintenance(s) : false;
  }

  showRetire(): boolean {
    const s = this.status();
    return s ? canRetire(s) : false;
  }

  showChangeLocation(): boolean {
    const s = this.status();
    return s ? canChangeLocation(s) : false;
  }
}
