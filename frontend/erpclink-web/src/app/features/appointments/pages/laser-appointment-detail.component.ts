import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Permissions } from '../../../core/permissions/permissions';
import { LaserAppointmentsApi } from '../../laser-clinic/services/laser-appointments-api.service';
import {
  CustomerPulseBalanceDto,
  LASER_APPOINTMENT_STATUS_BADGE,
  LASER_APPOINTMENT_STATUS_LABELS,
  LaserAppointmentDto,
  LaserAppointmentStatus,
  SessionDetailDto,
  appointmentPulsesConsumed,
  formatDateAr,
  formatTimeAr
} from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmService } from '../../../shared/components/confirm-dialog/confirm.service';

@Component({
  selector: 'app-laser-appointment-detail',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    HasPermissionDirective
  ],
  templateUrl: './laser-appointment-detail.component.html',
  styleUrl: './laser-appointment-detail.component.scss'
})
export class LaserAppointmentDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(LaserAppointmentsApi);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly detail = signal<SessionDetailDto | null>(null);
  readonly pulsesDraft = signal<number | null>(null);
  readonly formatDateAr = formatDateAr;
  readonly formatTimeAr = formatTimeAr;

  readonly form = new FormGroup({
    pulsesConsumed: new FormControl<number | null>(null, {
      validators: [Validators.min(1), Validators.max(5000)]
    }),
    durationMinutes: new FormControl<number | null>(null, {
      nonNullable: false,
      validators: [Validators.required, Validators.min(1), Validators.max(480)]
    }),
    amountPaid: new FormControl<number | null>(null, {
      validators: [Validators.min(0)]
    }),
    packagePrice: new FormControl<number | null>(null, {
      validators: [Validators.min(0)]
    })
  });

  readonly needsPackagePrice = computed(() => {
    const bal = this.pulseBalance();
    return this.hasPulsePackage() && (bal?.packagePriceTotal == null);
  });

  readonly item = computed(() => this.detail()?.appointment ?? null);
  readonly pulseBalance = computed(() => this.detail()?.pulseBalance ?? null);
  readonly totalAmountPaid = computed(() => this.detail()?.totalAmountPaid ?? 0);
  readonly availablePulses = computed(() => this.detail()?.availablePulses ?? 0);

  readonly hasPulsePackage = computed(() => {
    const a = this.item();
    if (!a) return false;
    return a.services.some((s) => s.serviceName.includes('نبضة'));
  });

  /** Remaining after applying the entered pulses (uses available = remaining + this session's prior record). */
  readonly remainingAfterSave = computed(() => {
    const balance = this.pulseBalance();
    if (!balance || balance.packageTotal == null) {
      return null;
    }
    const baseRemaining = this.availablePulses();
    const entered = this.pulsesDraft();
    if (entered == null || entered < 1) {
      return baseRemaining;
    }
    return Math.max(0, baseRemaining - entered);
  });

  readonly packageTotal = computed(() => this.pulseBalance()?.packageTotal ?? null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/appointments']);
      return;
    }
    this.load(id);
  }

  load(id: string): void {
    this.loading.set(true);
    this.api.getSession(id).subscribe({
      next: (row) => {
        this.detail.set(row);
        this.patchForm(row.appointment, row.pulseBalance, row.availablePulses);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('تعذّر تحميل تفاصيل الجلسة');
        void this.router.navigate(['/app/appointments']);
      }
    });
  }

  private patchForm(a: LaserAppointmentDto, balance: CustomerPulseBalanceDto, availablePulses: number): void {
    const recorded = appointmentPulsesConsumed(a);
    // New session / not recorded yet → default to remaining (available) pulses.
    const defaultPulses = recorded ?? (availablePulses > 0 ? availablePulses : null);
    this.pulsesDraft.set(defaultPulses);
    this.form.patchValue({
      pulsesConsumed: defaultPulses,
      durationMinutes: a.durationMinutes > 0 ? a.durationMinutes : null,
      amountPaid: a.amountPaid ?? null,
      packagePrice: balance.packagePriceTotal ?? null
    });
    if (a.services.some((s) => s.serviceName.includes('نبضة'))) {
      this.form.controls.pulsesConsumed.setValidators([
        Validators.required,
        Validators.min(1),
        Validators.max(5000)
      ]);
    } else {
      this.form.controls.pulsesConsumed.clearValidators();
    }
    this.form.controls.pulsesConsumed.updateValueAndValidity({ emitEvent: false });
  }

  onPulsesInput(): void {
    const raw = this.form.controls.pulsesConsumed.value;
    const n = Number(raw);
    this.pulsesDraft.set(Number.isFinite(n) && n > 0 ? Math.round(n) : null);
  }

  statusLabel(status: LaserAppointmentStatus): string {
    return LASER_APPOINTMENT_STATUS_LABELS[status];
  }

  badgeClass(status: LaserAppointmentStatus): string {
    return LASER_APPOINTMENT_STATUS_BADGE[status];
  }

  save(): void {
    const a = this.item();
    if (!a) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.error('أكملي مدة الجلسة وعدد النبضات والمبلغ إن لزم');
      return;
    }

    const raw = this.form.getRawValue();
    const durationMinutes = Number(raw.durationMinutes);
    const pulsesConsumed =
      raw.pulsesConsumed != null && Number(raw.pulsesConsumed) > 0 ? Math.round(Number(raw.pulsesConsumed)) : null;
    const amountRaw = raw.amountPaid;
    const amountPaid =
      amountRaw != null && `${amountRaw}`.trim() !== '' && Number.isFinite(Number(amountRaw))
        ? Number(amountRaw)
        : null;
    const packagePriceRaw = raw.packagePrice;
    const packagePrice =
      packagePriceRaw != null && `${packagePriceRaw}`.trim() !== '' && Number.isFinite(Number(packagePriceRaw))
        ? Number(packagePriceRaw)
        : null;

    if (this.hasPulsePackage() && (pulsesConsumed == null || pulsesConsumed < 1)) {
      this.toast.error('أدخلي عدد النبضات المستهلكة في الجلسة');
      return;
    }

    if (this.needsPackagePrice() && (packagePrice == null || packagePrice < 0)) {
      this.toast.error('أدخلي سعر الباكدج الإجمالي (مثال: 1000)');
      return;
    }

    if (this.hasPulsePackage() && pulsesConsumed != null) {
      const available = this.availablePulses();
      if (pulsesConsumed > available) {
        this.toast.error(`النبضات أكبر من المتبقي (متاح: ${available})`);
        return;
      }
    }

    this.saving.set(true);
    this.api
      .recordSession(a.id, {
        durationMinutes: Math.round(durationMinutes),
        pulsesConsumed,
        amountPaid: amountPaid != null && Number.isFinite(amountPaid) ? amountPaid : null,
        packagePrice: this.needsPackagePrice() ? packagePrice : null,
        markAttended: true
      })
      .subscribe({
        next: (row) => {
          this.detail.set(row);
          this.patchForm(row.appointment, row.pulseBalance, row.availablePulses);
          this.saving.set(false);
          this.toast.success(
            `تم حفظ الجلسة — المتبقي ${row.pulseBalance.remaining ?? 0} من ${row.pulseBalance.packageTotal ?? '—'}`
          );
        },
        error: (err) => {
          this.saving.set(false);
          const msg =
            err?.error?.detail || err?.error?.title || err?.error?.message || 'تعذّر حفظ الجلسة';
          this.toast.error(msg);
        }
      });
  }

  async cancel(): Promise<void> {
    const row = this.item();
    if (!row) return;
    const ok = await this.confirm.confirm({
      title: 'إلغاء الجلسة',
      message: `إلغاء جلسة ${row.customerName}؟`,
      variant: 'danger',
      confirmLabel: 'إلغاء الجلسة'
    });
    if (!ok) return;
    this.api.cancel(row.id).subscribe({
      next: () => {
        this.toast.success('تم إلغاء الجلسة');
        void this.router.navigate(['/app/customers', row.customerId, 'edit']);
      }
    });
  }

  backToCustomer(): void {
    const a = this.item();
    if (a) {
      void this.router.navigate(['/app/customers', a.customerId, 'edit']);
    } else {
      void this.router.navigate(['/app/customers']);
    }
  }
}
