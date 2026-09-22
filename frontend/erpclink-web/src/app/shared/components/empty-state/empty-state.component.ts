import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss'
})
export class EmptyStateComponent {
  readonly title = input.required<string>();
  readonly message = input<string | undefined>();
  readonly actionLabel = input<string | undefined>();

  readonly action = output<void>();
}
