import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-feature-shell',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent],
  templateUrl: './feature-shell.component.html',
  styleUrl: './feature-shell.component.scss'
})
export class FeatureShellComponent {
  private readonly route = inject(ActivatedRoute);

  readonly title = toSignal(this.route.data.pipe(map((data) => String(data['title'] ?? ''))), {
    initialValue: ''
  });

  readonly subtitle = toSignal(
    this.route.data.pipe(map((data) => data['subtitle'] as string | undefined)),
    { initialValue: undefined }
  );
}
