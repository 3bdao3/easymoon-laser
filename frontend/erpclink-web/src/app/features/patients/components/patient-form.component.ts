import { Component, input, output } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Gender, GENDER_LABELS } from '../models/patient.models';
import { PatientFormGroup } from '../utils/patient-form.factory';

@Component({
  selector: 'app-patient-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './patient-form.component.html',
  styleUrl: './patient-form.component.scss'
})
export class PatientFormComponent {
  readonly form = input.required<PatientFormGroup>();
  readonly submitting = input(false);

  readonly submitForm = output<void>();

  readonly genderOptions = (
    Object.entries(GENDER_LABELS) as [string, string][]
  ).map(([value, label]) => ({
    value: Number(value) as Gender,
    label
  }));

  onSubmit(): void {
    this.submitForm.emit();
  }
}
