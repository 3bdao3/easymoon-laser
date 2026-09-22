import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ToastService } from '../../../core/services/toast.service';
import { trimToNull } from '../../../shared/utils/form.util';
import { SpecialtiesApi } from '../services/specialties.api';

@Component({
  selector: 'app-specialty-create',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent],
  templateUrl: './specialty-create.component.html'
})
export class SpecialtyCreateComponent {
  private readonly api = inject(SpecialtiesApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly submitting = signal(false);
  readonly form = new FormGroup({
    code: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true })
  });

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.submitting.set(true);
    this.api
      .create({
        code: v.code.trim(),
        name: v.name.trim(),
        description: trimToNull(v.description)
      })
      .subscribe({
        next: () => {
          this.toast.success('تم إنشاء التخصص');
          void this.router.navigate(['/app/specialties']);
        },
        error: () => this.submitting.set(false)
      });
  }
}
