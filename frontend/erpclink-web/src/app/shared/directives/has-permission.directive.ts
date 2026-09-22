import {
  Directive,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  input
} from '@angular/core';
import { PermissionService } from '../../core/permissions/permission.service';

@Directive({
  selector: '[appHasPermission]',
  standalone: true
})
export class HasPermissionDirective {
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly permissions = inject(PermissionService);

  readonly appHasPermission = input.required<string | string[]>();

  constructor() {
    effect(() => {
      const required = this.appHasPermission();
      const codes = Array.isArray(required) ? required : [required];
      const allowed = codes.some((code) => this.permissions.has(code));

      this.viewContainer.clear();
      if (allowed) {
        this.viewContainer.createEmbeddedView(this.templateRef);
      }
    });
  }
}
