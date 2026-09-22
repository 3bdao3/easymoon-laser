import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { CustomersApi } from '../../laser-clinic/services/customers-api.service';
import { LaserServicesApi } from '../../laser-clinic/services/laser-services-api.service';
import { LaserAppointmentsApi } from '../../laser-clinic/services/laser-appointments-api.service';
import {
  AvailabilityResultDto,
  AvailabilitySlotDto,
  AvailabilitySlotStatus,
  CustomerDto,
  LaserServiceDto,
  formatTime,
  toApiTime
} from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ToastService } from '../../../core/services/toast.service';

type WizardStep = 1 | 2 | 3 | 4;

@Component({
  selector: 'app-book-wizard',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './book-wizard.component.html',
  styleUrl: './book-wizard.component.scss'
})
export class BookWizardComponent implements OnInit {
  private readonly customersApi = inject(CustomersApi);
  private readonly servicesApi = inject(LaserServicesApi);
  private readonly appointmentsApi = inject(LaserAppointmentsApi);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly step = signal<WizardStep>(1);
  readonly loadingCustomers = signal(false);
  readonly loadingServices = signal(false);
  readonly loadingSlots = signal(false);
  readonly submitting = signal(false);

  readonly customers = signal<CustomerDto[]>([]);
  readonly services = signal<LaserServiceDto[]>([]);
  readonly selectedCustomer = signal<CustomerDto | null>(null);
  readonly selectedServiceIds = signal<string[]>([]);
  readonly availability = signal<AvailabilityResultDto | null>(null);
  readonly selectedSlot = signal<AvailabilitySlotDto | null>(null);
  readonly notes = new FormControl('', { nonNullable: true });
  readonly customerQuery = new FormControl('', { nonNullable: true });
  readonly dateControl = new FormControl(new Date().toISOString().slice(0, 10), { nonNullable: true });

  readonly formatTime = formatTime;

  readonly selectedServices = computed(() => {
    const ids = new Set(this.selectedServiceIds());
    return this.services().filter((s) => ids.has(s.id));
  });

  readonly estimatedDuration = computed(() => {
    const avail = this.availability();
    if (avail) {
      return avail.clinicalDurationMinutes;
    }
    return this.selectedServices().reduce((sum, s) => sum + s.recommendedDurationMinutes, 0);
  });

  ngOnInit(): void {
    this.loadCustomers();
    this.loadServices();
    this.customerQuery.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => this.loadCustomers());
    this.dateControl.valueChanges.subscribe(() => {
      this.selectedSlot.set(null);
      if (this.step() === 3) {
        this.loadSlots();
      }
    });
  }

  loadCustomers(): void {
    this.loadingCustomers.set(true);
    this.customersApi.search(this.customerQuery.value.trim() || undefined, true).subscribe({
      next: (rows) => {
        this.customers.set(rows);
        this.loadingCustomers.set(false);
      },
      error: () => this.loadingCustomers.set(false)
    });
  }

  loadServices(): void {
    this.loadingServices.set(true);
    this.servicesApi.list(true).subscribe({
      next: (rows) => {
        this.services.set(rows);
        this.loadingServices.set(false);
      },
      error: () => this.loadingServices.set(false)
    });
  }

  selectCustomer(c: CustomerDto): void {
    this.selectedCustomer.set(c);
  }

  toggleService(id: string): void {
    const current = this.selectedServiceIds();
    if (current.includes(id)) {
      this.selectedServiceIds.set(current.filter((x) => x !== id));
    } else {
      this.selectedServiceIds.set([...current, id]);
    }
    this.selectedSlot.set(null);
    this.availability.set(null);
  }

  isServiceSelected(id: string): boolean {
    return this.selectedServiceIds().includes(id);
  }

  canGoNext(): boolean {
    const s = this.step();
    if (s === 1) return !!this.selectedCustomer();
    if (s === 2) return this.selectedServiceIds().length > 0;
    if (s === 3) return !!this.selectedSlot();
    return false;
  }

  goNext(): void {
    const s = this.step();
    if (s === 1 && !this.selectedCustomer()) {
      this.toast.error('اختر عميلاً');
      return;
    }
    if (s === 2 && this.selectedServiceIds().length === 0) {
      this.toast.error('اختر خدمة واحدة على الأقل');
      return;
    }
    if (s === 2) {
      this.step.set(3);
      this.loadSlots();
      return;
    }
    if (s === 3 && !this.selectedSlot()) {
      this.toast.error('اختر موعداً متاحاً');
      return;
    }
    if (s < 4) {
      this.step.set((s + 1) as WizardStep);
    }
  }

  goBack(): void {
    const s = this.step();
    if (s > 1) {
      this.step.set((s - 1) as WizardStep);
    }
  }

  loadSlots(): void {
    const date = this.dateControl.value;
    const ids = this.selectedServiceIds();
    if (!date || ids.length === 0) {
      return;
    }
    this.loadingSlots.set(true);
    this.appointmentsApi.getAvailability(date, ids).subscribe({
      next: (result) => {
        this.availability.set(result);
        this.loadingSlots.set(false);
      },
      error: () => this.loadingSlots.set(false)
    });
  }

  selectSlot(slot: AvailabilitySlotDto): void {
    if (slot.status !== AvailabilitySlotStatus.Available) {
      return;
    }
    this.selectedSlot.set(slot);
  }

  submit(): void {
    const customer = this.selectedCustomer();
    const slot = this.selectedSlot();
    const date = this.dateControl.value;
    const ids = this.selectedServiceIds();
    if (!customer || !slot || !date || ids.length === 0) {
      return;
    }
    this.submitting.set(true);
    this.appointmentsApi
      .create({
        customerId: customer.id,
        appointmentDate: date,
        startTime: toApiTime(slot.startTime),
        laserServiceIds: ids,
        notes: this.notes.value.trim() || null
      })
      .subscribe({
        next: (appt) => {
          this.toast.success('تم تأكيد الحجز');
          this.submitting.set(false);
          void this.router.navigate(['/app/appointments', appt.id]);
        },
        error: () => this.submitting.set(false)
      });
  }

  stepClass(n: WizardStep): string {
    const current = this.step();
    if (n === current) {
      return 'wizard-step wizard-step--active';
    }
    if (n < current) {
      return 'wizard-step wizard-step--done';
    }
    return 'wizard-step';
  }

  selectedServiceNames(): string {
    return this.selectedServices()
      .map((s) => s.name)
      .join('، ');
  }

  availableSlots(): AvailabilitySlotDto[] {
    return (this.availability()?.slots ?? []).filter((s) => s.status === AvailabilitySlotStatus.Available);
  }

  hasSlots(): boolean {
    return this.availableSlots().length > 0;
  }
}
