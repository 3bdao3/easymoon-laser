import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Permissions } from '../../../core/permissions/permissions';
import { ClinicSettingsApi } from '../../laser-clinic/services/clinic-settings-api.service';
import { formatTime, toApiTime } from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { PermissionService } from '../../../core/permissions/permission.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-clinic-settings',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    HasPermissionDirective
  ],
  templateUrl: './clinic-settings.component.html'
})
export class ClinicSettingsComponent implements OnInit {
  private readonly api = inject(ClinicSettingsApi);
  private readonly toast = inject(ToastService);
  private readonly permissionService = inject(PermissionService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly canManage = this.permissionService.has(Permissions.LaserSettingsManage);

  readonly form = new FormGroup({
    openingTime: new FormControl('09:00', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    closingTime: new FormControl('21:00', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    appointmentSlotIntervalMinutes: new FormControl(15, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(5)]
    }),
    defaultBufferMinutes: new FormControl(10, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)]
    })
  });

  ngOnInit(): void {
    this.api.get().subscribe({
      next: (s) => {
        this.form.patchValue({
          openingTime: formatTime(s.openingTime),
          closingTime: formatTime(s.closingTime),
          appointmentSlotIntervalMinutes: s.appointmentSlotIntervalMinutes,
          defaultBufferMinutes: s.defaultBufferMinutes
        });
        if (!this.canManage) {
          this.form.disable();
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  submit(): void {
    if (!this.canManage || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    this.saving.set(true);
    this.api
      .update({
        openingTime: toApiTime(raw.openingTime),
        closingTime: toApiTime(raw.closingTime),
        appointmentSlotIntervalMinutes: raw.appointmentSlotIntervalMinutes,
        defaultBufferMinutes: raw.defaultBufferMinutes
      })
      .subscribe({
        next: () => {
          this.toast.success('تم حفظ إعدادات العيادة');
          this.saving.set(false);
        },
        error: () => this.saving.set(false)
      });
  }
}
