import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ToastService } from '../../../core/services/toast.service';
import { trimToNull } from '../../../shared/utils/form.util';
import { UsersApi } from '../services/users.api';

@Component({
  selector: 'app-user-edit',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './user-edit.component.html'
})
export class UserEditComponent implements OnInit {
  private readonly api = inject(UsersApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  private userId = '';

  readonly form = new FormGroup({
    fullName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.email] }),
    phoneNumber: new FormControl('', { nonNullable: true })
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/administration']);
      return;
    }
    this.userId = id;
    this.api.getById(id).subscribe({
      next: (user) => {
        this.form.patchValue({
          fullName: user.fullName,
          email: user.email,
          phoneNumber: user.phoneNumber ?? ''
        });
        this.loading.set(false);
      },
      error: () => void this.router.navigate(['/app/administration'])
    });
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.submitting.set(true);
    this.api
      .update(this.userId, {
        fullName: v.fullName.trim(),
        email: trimToNull(v.email) ?? undefined,
        phoneNumber: trimToNull(v.phoneNumber)
      })
      .subscribe({
        next: () => {
          this.toast.success('تم الحفظ');
          void this.router.navigate(['/app/administration']);
        },
        error: () => this.submitting.set(false)
      });
  }
}
