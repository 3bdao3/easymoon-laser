import { Component, input, output, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { LoadingService } from '../../core/services/loading.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [LoadingSpinnerComponent],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss'
})
export class TopbarComponent {
  readonly menuToggle = output<void>();
  readonly sidebarOpen = input(false);

  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly loading = inject(LoadingService);

  readonly user = this.auth.user;

  toggleMenu(): void {
    this.menuToggle.emit();
  }

  initials(name: string | null | undefined): string {
    if (!name?.trim()) return 'EM';
    const parts = name.trim().split(/\s+/).slice(0, 2);
    return parts.map((p) => p.charAt(0)).join('').toUpperCase();
  }

  logout(): void {
    this.auth.logout().subscribe({
      next: () => void this.router.navigate(['/login']),
      error: () => {
        this.auth.clearSession();
        void this.router.navigate(['/login']);
      }
    });
  }
}
