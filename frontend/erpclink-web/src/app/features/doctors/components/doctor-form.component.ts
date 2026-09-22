import { Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { SpecialtyDto } from '../../specialties/models/specialty.models';

@Component({
  selector: 'app-doctor-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './doctor-form.component.html'
})
export class DoctorFormComponent {
  readonly form = input.required<FormGroup>();
  readonly specialties = input<SpecialtyDto[]>([]);
  readonly submitting = input(false);
  readonly submitForm = output<void>();

  onSubmit(): void {
    this.submitForm.emit();
  }
}
