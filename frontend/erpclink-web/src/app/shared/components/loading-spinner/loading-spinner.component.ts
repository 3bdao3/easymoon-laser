import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  templateUrl: './loading-spinner.component.html',
  styleUrl: './loading-spinner.component.scss'
})
export class LoadingSpinnerComponent {
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly label = input<string>('جاري التحميل…');
}
