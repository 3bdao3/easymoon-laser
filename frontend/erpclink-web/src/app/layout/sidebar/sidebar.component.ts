import { Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { PermissionService } from '../../core/permissions/permission.service';
import { MAIN_NAV_ITEMS } from '../navigation/nav-items';
import { NavItem } from '../navigation/nav-item.model';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  readonly open = input(false);
  readonly closeRequested = output<void>();

  constructor(private readonly permissions: PermissionService) {}

  visibleItems(): NavItem[] {
    return MAIN_NAV_ITEMS.filter((item) => {
      if (item.requiredAnyPermission?.length) {
        return item.requiredAnyPermission.some((p) => this.permissions.has(p));
      }
      return !item.requiredPermission || this.permissions.has(item.requiredPermission);
    });
  }

  onNavigate(): void {
    this.closeRequested.emit();
  }
}
