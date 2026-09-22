import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ToastService } from '../../../core/services/toast.service';
import { trimToNull } from '../../../shared/utils/form.util';
import { SpecialtiesApi } from '../services/specialties.api';

@Component({
  selector: 'app-specialty-edit',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './specialty-edit.component.html'
})
export class SpecialtyEditComponent implements OnInit {
  private readonly api = inject(SpecialtiesApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly code = signal('');
  private specialtyId = '';

  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true })
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/specialties']);
      return;
    }
    this.specialtyId = id;
    this.api.list().subscribe({
      next: (list) => {
        const item = list.find((s) => s.id === id);
        if (!item) {
          void this.router.navigate(['/app/specialties']);
          return;
        }
        this.code.set(item.code);
        this.form.patchValue({
          name: item.name,
          description: item.description ?? ''
        });
        this.loading.set(false);
      },
      error: () => void this.router.navigate(['/app/specialties'])
    });
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.submitting.set(true);
    this.api
      .update(this.specialtyId, {
        name: v.name.trim(),
        description: trimToNull(v.description)
      })
      .subscribe({
        next: () => {
          this.toast.success('تم الحفظ');
          void this.router.navigate(['/app/specialties']);
        },
        error: () => this.submitting.set(false)
      });
  }
}
