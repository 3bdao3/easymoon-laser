import { Component, inject } from '@angular/core';
import { ConfirmService } from './confirm.service';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss'
})
export class ConfirmDialogComponent {
  readonly confirmService = inject(ConfirmService);

  onBackdropClick(): void {
    this.confirmService.resolve(false);
  }

  onConfirm(): void {
    this.confirmService.resolve(true);
  }

  onCancel(): void {
    this.confirmService.resolve(false);
  }
}
