import { Component, inject, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Permissions } from '../../../core/permissions/permissions';
import { PermissionService } from '../../../core/permissions/permission.service';
import { ToastService } from '../../../core/services/toast.service';
import { LaserServiceDto } from '../../laser-clinic/models/laser-clinic.models';
import { LaserServicesApi } from '../../laser-clinic/services/laser-services-api.service';
import { ConfirmService } from '../../../shared/components/confirm-dialog/confirm.service';

@Component({
  selector: 'app-laser-service-admin-card',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './laser-service-admin-card.component.html',
  styleUrl: './laser-service-admin-card.component.scss'
})
export class LaserServiceAdminCardComponent {
  private readonly api = inject(LaserServicesApi);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly permissions = inject(PermissionService);

  readonly catalogChanged = output<void>();
  readonly canManage = this.permissions.has(Permissions.LaserServicesManage);
  readonly open = signal(false);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly items = signal<LaserServiceDto[]>([]);
  readonly prices = signal<Record<string, number>>({});

  readonly addForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    durationMinutes: new FormControl(15, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1)]
    }),
    price: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] })
  });

  toggle(): void {
    const next = !this.open();
    this.open.set(next);
    if (next) {
      this.load();
    }
  }

  priceOf(id: string): number {
    return this.prices()[id] ?? 0;
  }

  setPrice(id: string, raw: string): void {
    const parsed = Number(raw);
    this.prices.update((current) => ({
      ...current,
      [id]: Number.isFinite(parsed) && parsed >= 0 ? parsed : 0
    }));
  }

  add(): void {
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }
    const raw = this.addForm.getRawValue();
    const duration = Math.round(raw.durationMinutes);
    this.saving.set(true);
    this.api
      .create({
        name: raw.name.trim(),
        minDurationMinutes: duration,
        maxDurationMinutes: duration,
        displayOrder: this.items().length + 1,
        price: raw.price,
        notes: null
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.addForm.reset({ name: '', durationMinutes: 15, price: 0 });
          this.toast.success('اتضافت المنطقة');
          this.load();
          this.catalogChanged.emit();
        },
        error: () => this.saving.set(false)
      });
  }

  savePrice(row: LaserServiceDto): void {
    const price = this.priceOf(row.id);
    this.saving.set(true);
    this.api
      .update(row.id, {
        name: row.name,
        minDurationMinutes: row.minDurationMinutes,
        maxDurationMinutes: row.maxDurationMinutes,
        displayOrder: row.displayOrder,
        price,
        notes: row.notes,
        isActive: row.isActive
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.toast.success('اتحفظ السعر');
          this.load();
          this.catalogChanged.emit();
        },
        error: () => this.saving.set(false)
      });
  }

  async remove(row: LaserServiceDto): Promise<void> {
    const ok = await this.confirm.confirm({
      title: 'حذف المنطقة',
      message: `تمسح «${row.name}» من المناطق؟`,
      confirmLabel: 'حذف',
      variant: 'danger'
    });
    if (!ok) {
      return;
    }
    this.saving.set(true);
    this.api.delete(row.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('اتشالت المنطقة');
        this.load();
        this.catalogChanged.emit();
      },
      error: () => this.saving.set(false)
    });
  }

  private load(): void {
    this.loading.set(true);
    this.api.list(true).subscribe({
      next: (rows) => {
        this.items.set(rows);
        const prices: Record<string, number> = {};
        for (const row of rows) {
          prices[row.id] = row.price ?? 0;
        }
        this.prices.set(prices);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
