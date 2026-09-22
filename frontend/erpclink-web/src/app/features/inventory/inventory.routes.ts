import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const INVENTORY_ROUTES: Routes = [
  {
    path: 'warehouses',
    canActivate: [permissionGuard(Permissions.InventoryWarehousesView)],
    loadComponent: () =>
      import('./pages/warehouse-list/warehouse-list.component').then((m) => m.WarehouseListComponent)
  },
  {
    path: 'items',
    canActivate: [permissionGuard(Permissions.InventoryItemsView)],
    loadComponent: () =>
      import('./pages/item-list/item-list.component').then((m) => m.ItemListComponent)
  },
  {
    path: 'stock',
    canActivate: [permissionGuard(Permissions.InventoryStockView)],
    loadComponent: () =>
      import('./pages/stock-balances/stock-balances.component').then((m) => m.StockBalancesComponent)
  },
  {
    path: 'goods-receipts/new',
    canActivate: [permissionGuard(Permissions.InventoryGoodsReceiptsCreate)],
    loadComponent: () =>
      import('./pages/goods-receipt-form/goods-receipt-form.component').then((m) => m.GoodsReceiptFormComponent)
  },
  {
    path: 'valuation',
    canActivate: [permissionGuard(Permissions.InventoryValuationView)],
    loadComponent: () =>
      import('./pages/inventory-valuation/inventory-valuation.component').then((m) => m.InventoryValuationComponent)
  },
  {
    path: 'cost-layers',
    canActivate: [permissionGuard(Permissions.InventoryCostLayersView)],
    loadComponent: () =>
      import('./pages/cost-layers/cost-layers.component').then((m) => m.CostLayersComponent)
  },
  {
    path: 'cost-history',
    canActivate: [permissionGuard(Permissions.InventoryCostHistoryView)],
    loadComponent: () =>
      import('./pages/cost-history/cost-history.component').then((m) => m.CostHistoryComponent)
  },
  {
    path: 'issue-costs',
    canActivate: [permissionGuard(Permissions.InventoryIssueCostView)],
    loadComponent: () =>
      import('./pages/issue-costs/issue-costs.component').then((m) => m.IssueCostsComponent)
  },
  { path: '', pathMatch: 'full', redirectTo: 'warehouses' }
];
