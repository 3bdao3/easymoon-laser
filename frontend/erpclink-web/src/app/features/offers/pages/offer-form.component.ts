import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LaserOffersApi } from '../../laser-clinic/services/laser-offers-api.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-offer-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './offer-form.component.html'
})
export class OfferFormComponent implements OnInit {
  private readonly api = inject(LaserOffersApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly offerId = signal<string | null>(null);

  readonly form = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true }),
    price: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)]
    }),
    validFrom: new FormControl<string | null>(null),
    validTo: new FormControl<string | null>(null),
    displayOrder: new FormControl(0, { nonNullable: true }),
    isActive: new FormControl(true, { nonNullable: true })
  });

  get isEdit(): boolean {
    return !!this.offerId();
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }
    this.offerId.set(id);
    this.loading.set(true);
    this.api.getById(id).subscribe({
      next: (o) => {
        this.form.patchValue({
          title: o.title,
          description: o.description ?? '',
          price: o.price,
          validFrom: o.validFrom ? o.validFrom.slice(0, 10) : null,
          validTo: o.validTo ? o.validTo.slice(0, 10) : null,
          displayOrder: o.displayOrder,
          isActive: o.isActive
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.error('أكملي اسم العرض والسعر');
      return;
    }
    const raw = this.form.getRawValue();
    if (raw.validFrom && raw.validTo && raw.validTo < raw.validFrom) {
      this.toast.error('تاريخ نهاية العرض يجب أن يكون بعد البداية');
      return;
    }

    this.saving.set(true);
    const id = this.offerId();
    const payload = {
      title: raw.title.trim(),
      description: raw.description.trim() || null,
      price: Number(raw.price),
      validFrom: raw.validFrom || null,
      validTo: raw.validTo || null,
      displayOrder: raw.displayOrder
    };

    if (id) {
      this.api
        .update(id, { ...payload, isActive: raw.isActive })
        .subscribe({
          next: () => {
            this.toast.success('تم تحديث العرض');
            this.saving.set(false);
            void this.router.navigate(['/app/offers']);
          },
          error: () => this.saving.set(false)
        });
    } else {
      this.api.create(payload).subscribe({
        next: () => {
          this.toast.success('تم إضافة العرض');
          this.saving.set(false);
          void this.router.navigate(['/app/offers']);
        },
        error: () => this.saving.set(false)
      });
    }
  }
}
