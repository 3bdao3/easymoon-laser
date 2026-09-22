import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ProcurementApiService } from '../../services/procurement-api.service';
import { buildSupplierFormValidators } from '../../suppliers/supplier-form.utils';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-supplier-form',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  templateUrl: './supplier-form.component.html',
  styleUrls: ['./supplier-form.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class SupplierFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ProcurementApiService);
  private readonly router = inject(Router);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly validators = buildSupplierFormValidators();
  readonly form = this.fb.nonNullable.group({
    name: ['', this.validators.name],
    contactName: [''],
    phone: [''],
    email: ['', this.validators.email],
    address: [''],
    notes: ['']
  });

  submit(): void {
    if (this.form.invalid) return;
    const raw = this.form.getRawValue();
    this.api
      .createSupplier({
        name: raw.name,
        contactName: raw.contactName || null,
        phone: raw.phone || null,
        email: raw.email || null,
        address: raw.address || null,
        notes: raw.notes || null
      })
      .subscribe({
        next: (s) => void this.router.navigate(['/app/procurement/suppliers', s.id]),
        error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
      });
  }
}
