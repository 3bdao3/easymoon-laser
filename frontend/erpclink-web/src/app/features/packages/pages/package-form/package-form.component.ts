import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PackagesApiService } from '../../services/packages-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-package-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent],
  templateUrl: './package-form.component.html',
  styleUrls: ['./package-form.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class PackageFormComponent {
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(PackagesApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly submitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    packageCode: ['', [Validators.required, Validators.maxLength(64)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['']
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    const raw = this.form.getRawValue();
    this.api
      .create({
        packageCode: raw.packageCode,
        name: raw.name,
        description: raw.description || null,
        items: null
      })
      .subscribe({
        next: (pkg) => {
          this.submitting.set(false);
          this.toast.success('تم إنشاء الباقة.');
          void this.router.navigate(['/app/packages', pkg.id]);
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          this.toast.error(this.errors.resolveMessage(err));
        }
      });
  }
}
