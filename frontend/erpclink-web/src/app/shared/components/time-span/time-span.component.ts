import { Component, input } from '@angular/core';
import { formatTimeAr } from '../../../features/laser-clinic/models/laser-clinic.models';

@Component({
  selector: 'app-time-span',
  standalone: true,
  template: `
    <div class="time-span">
      <div class="time-span__item">
        <span class="time-span__label">من</span>
        <span class="time-span__time">{{ formatTimeAr(start()) }}</span>
      </div>
      <div class="time-span__item">
        <span class="time-span__label">إلى</span>
        <span class="time-span__time">{{ formatTimeAr(end()) }}</span>
      </div>
    </div>
  `,
  styles: `
    .time-span {
      display: inline-flex;
      gap: 0.85rem;
    }

    .time-span__item {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.15rem;
      min-width: 4.5rem;
    }

    .time-span__label {
      font-size: 0.7rem;
      font-weight: 700;
      color: var(--text-secondary);
      line-height: 1;
    }

    .time-span__time {
      font-weight: 800;
      font-size: 0.82rem;
      white-space: nowrap;
      line-height: 1.2;
    }
  `
})
export class TimeSpanComponent {
  readonly start = input<string | null | undefined>();
  readonly end = input<string | null | undefined>();
  readonly formatTimeAr = formatTimeAr;
}
