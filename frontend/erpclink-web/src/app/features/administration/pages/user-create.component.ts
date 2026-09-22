import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ToastService } from '../../../core/services/toast.service';
import { trimToNull } from '../../../shared/utils/form.util';
import { UsersApi } from '../services/users.api';

@Component({
  selector: 'app-user-create',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent],
  templateUrl: './user-create.component.html'
})
export class UserCreateComponent {
  private readonly api = inject(UsersApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly submitting = signal(false);
  readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6)] }),
    fullName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    phoneNumber: new FormControl('', { nonNullable: true }),
    roles: new FormControl('', { nonNullable: true })
  });

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const rolesRaw = v.roles.trim();
    const roles = rolesRaw
      ? rolesRaw
          .split(',')
          .map((r) => r.trim())
          .filter((r) => r.length > 0)
      : null;

    this.submitting.set(true);
    this.api
      .create({
        email: v.email.trim(),
        password: v.password,
        fullName: v.fullName.trim(),
        phoneNumber: trimToNull(v.phoneNumber),
        roles
      })
      .subscribe({
        next: () => {
          this.toast.success('تم إنشاء المستخدم');
          void this.router.navigate(['/app/administration']);
        },
        error: () => this.submitting.set(false)
      });
  }
}
