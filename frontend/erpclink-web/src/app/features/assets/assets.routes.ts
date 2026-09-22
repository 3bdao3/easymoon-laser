import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const ASSETS_ROUTES: Routes = [
  {
    path: 'categories',
    canActivate: [permissionGuard(Permissions.AssetsCategoriesView)],
    loadComponent: () =>
      import('./pages/category-list/category-list.component').then((m) => m.CategoryListComponent)
  },
  {
    path: 'locations',
    canActivate: [permissionGuard(Permissions.AssetsLocationsView)],
    loadComponent: () =>
      import('./pages/location-list/location-list.component').then((m) => m.LocationListComponent)
  },
  {
    path: 'accounting',
    canActivate: [permissionGuard(Permissions.AssetsAccountingView)],
    loadComponent: () =>
      import('./pages/asset-accounting-list/asset-accounting-list.component').then(
        (m) => m.AssetAccountingListComponent
      )
  },
  {
    path: 'accounting/:assetId',
    canActivate: [permissionGuard(Permissions.AssetsAccountingView)],
    loadComponent: () =>
      import('./pages/asset-accounting-detail/asset-accounting-detail.component').then(
        (m) => m.AssetAccountingDetailComponent
      )
  },
  {
    path: 'valuation',
    canActivate: [permissionGuard(Permissions.AssetsAccountingView)],
    loadComponent: () =>
      import('./pages/asset-valuation/asset-valuation.component').then((m) => m.AssetValuationComponent)
  },
  {
    path: 'disposals',
    canActivate: [permissionGuard(Permissions.AssetsDisposalView)],
    loadComponent: () =>
      import('./pages/asset-disposals/asset-disposals.component').then((m) => m.AssetDisposalsComponent)
  },
  {
    path: '',
    canActivate: [permissionGuard(Permissions.AssetsView)],
    loadComponent: () =>
      import('./pages/asset-list/asset-list.component').then((m) => m.AssetListComponent)
  },
  {
    path: ':id',
    canActivate: [permissionGuard(Permissions.AssetsView)],
    loadComponent: () =>
      import('./pages/asset-detail/asset-detail.component').then((m) => m.AssetDetailComponent)
  }
];
