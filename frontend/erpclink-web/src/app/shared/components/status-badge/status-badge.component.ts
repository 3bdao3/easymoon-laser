import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  templateUrl: './status-badge.component.html',
  styleUrl: './status-badge.component.scss'
})
export class StatusBadgeComponent {
  readonly status = input.required<string>();

  readonly modifier = computed(() => {
    const normalized = this.status().trim().toLowerCase().replace(/\s+/g, '-');
    return normalized || 'unknown';
  });
}
