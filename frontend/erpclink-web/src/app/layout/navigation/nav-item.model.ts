import { PermissionCode } from '../../core/permissions/permissions';

export interface NavItem {
  labelAr: string;
  labelEn: string;
  route: string;
  requiredPermission?: PermissionCode;
  /** When set, user needs any one of these permissions (overrides requiredPermission). */
  requiredAnyPermission?: PermissionCode[];
  icon?: string;
}
