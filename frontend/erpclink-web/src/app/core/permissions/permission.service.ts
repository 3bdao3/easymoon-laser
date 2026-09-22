import { Injectable } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { PermissionCode } from './permissions';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  constructor(private readonly auth: AuthService) {}

  has(permission: PermissionCode | string): boolean {
    return this.auth.hasPermission(permission);
  }

  hasAny(...permissions: (PermissionCode | string)[]): boolean {
    return this.auth.hasAnyPermission(...permissions);
  }
}
