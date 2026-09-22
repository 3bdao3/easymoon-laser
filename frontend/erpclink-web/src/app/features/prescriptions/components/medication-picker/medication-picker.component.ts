import { Component, inject, input, output, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MedicationsApiService } from '../../../medications/services/medications-api.service';
import { MedicationDto } from '../../../medications/models/medication.models';

@Component({
  selector: 'app-medication-picker',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './medication-picker.component.html',
  styleUrls: [
    './medication-picker.component.scss',
    '../../../medical-visits/_feature-layout.scss',
    '../../_feature-layout.scss'
  ]
})
export class MedicationPickerComponent {
  private readonly medicationsApi = inject(MedicationsApiService);

  readonly label = input('بحث عن دواء');
  readonly disabled = input(false);

  readonly medicationSelected = output<MedicationDto>();

  readonly query = new FormControl('', { nonNullable: true });
  readonly results = signal<MedicationDto[]>([]);
  readonly searching = signal(false);
  readonly selected = signal<MedicationDto | null>(null);

  constructor() {
    this.query.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        filter((q) => q.trim().length >= 2),
        switchMap((q) => {
          this.searching.set(true);
          return this.medicationsApi.search({ q: q.trim(), activeOnly: true, pageSize: 15 });
        }),
        takeUntilDestroyed()
      )
      .subscribe({
        next: (page) => {
          this.results.set(page.items);
          this.searching.set(false);
        },
        error: () => {
          this.results.set([]);
          this.searching.set(false);
        }
      });
  }

  pick(med: MedicationDto): void {
    this.selected.set(med);
    this.medicationSelected.emit(med);
    this.results.set([]);
    this.query.setValue('');
  }

  clear(): void {
    this.selected.set(null);
  }
}
