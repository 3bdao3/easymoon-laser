namespace ErpClink.BuildingBlocks.Application.Common;

/// <summary>
/// Well-known permission codes used for seeding and authorization attributes.
/// Authorization itself is data-driven via roles ↔ permissions.
/// </summary>
public static class PermissionCodes
{
    public const string AdministrationUsersView = "Administration.Users.View";
    public const string AdministrationUsersCreate = "Administration.Users.Create";
    public const string AdministrationUsersUpdate = "Administration.Users.Update";
    public const string AdministrationUsersDelete = "Administration.Users.Delete";
    public const string AdministrationRolesManage = "Administration.Roles.Manage";
    public const string AdministrationPermissionsView = "Administration.Permissions.View";

    public const string PatientsView = "Patients.View";
    public const string PatientsCreate = "Patients.Create";
    public const string PatientsUpdate = "Patients.Update";
    public const string PatientsDelete = "Patients.Delete";
    public const string PatientsActivate = "Patients.Activate";
    public const string PatientsDeactivate = "Patients.Deactivate";
    public const string PatientsAllergiesView = "Patients.Allergies.View";
    public const string PatientsAllergiesManage = "Patients.Allergies.Manage";
    public const string PatientsDocumentsView = "Patients.Documents.View";
    public const string PatientsDocumentsManage = "Patients.Documents.Manage";

    public const string DoctorsView = "Doctors.View";
    public const string DoctorsCreate = "Doctors.Create";
    public const string DoctorsUpdate = "Doctors.Update";
    public const string DoctorsActivate = "Doctors.Activate";
    public const string DoctorsDeactivate = "Doctors.Deactivate";
    public const string DoctorsAssignClinic = "Doctors.AssignClinic";

    public const string ClinicsView = "Clinics.View";
    public const string ClinicsCreate = "Clinics.Create";
    public const string ClinicsUpdate = "Clinics.Update";
    public const string ClinicsActivate = "Clinics.Activate";
    public const string ClinicsDeactivate = "Clinics.Deactivate";

    public const string SpecialtiesView = "Specialties.View";
    public const string SpecialtiesManage = "Specialties.Manage";

    public const string SchedulingView = "Scheduling.View";
    public const string SchedulingCreate = "Scheduling.Create";
    public const string SchedulingUpdate = "Scheduling.Update";
    public const string SchedulingActivate = "Scheduling.Activate";
    public const string SchedulingDeactivate = "Scheduling.Deactivate";
    public const string SchedulingAvailabilityView = "Scheduling.Availability.View";

    public const string AppointmentsView = "Appointments.View";
    public const string AppointmentsCreate = "Appointments.Create";
    public const string AppointmentsUpdate = "Appointments.Update";
    public const string AppointmentsConfirm = "Appointments.Confirm";
    public const string AppointmentsCancel = "Appointments.Cancel";
    public const string AppointmentsReschedule = "Appointments.Reschedule";
    public const string AppointmentsNoShow = "Appointments.NoShow";

    public const string QueueView = "Queue.View";
    public const string QueueCheckIn = "Queue.CheckIn";
    public const string QueueCall = "Queue.Call";
    public const string QueueStartService = "Queue.StartService";
    public const string QueueComplete = "Queue.Complete";
    public const string QueueSkip = "Queue.Skip";
    public const string QueueCancel = "Queue.Cancel";

    public const string MedicalVisitsView = "MedicalVisits.View";
    public const string MedicalVisitsCreate = "MedicalVisits.Create";
    public const string MedicalVisitsUpdate = "MedicalVisits.Update";
    public const string MedicalVisitsComplete = "MedicalVisits.Complete";
    public const string MedicalVisitsCancel = "MedicalVisits.Cancel";

    public const string PrescriptionsView = "Prescriptions.View";
    public const string PrescriptionsCreate = "Prescriptions.Create";
    public const string PrescriptionsUpdate = "Prescriptions.Update";
    public const string PrescriptionsIssue = "Prescriptions.Issue";
    public const string PrescriptionsCancel = "Prescriptions.Cancel";

    public const string MedicationsView = "Medications.View";
    public const string MedicationsCreate = "Medications.Create";
    public const string MedicationsUpdate = "Medications.Update";
    public const string MedicationsActivate = "Medications.Activate";
    public const string MedicationsDeactivate = "Medications.Deactivate";

    public const string ServicesView = "Services.View";
    public const string ServicesCreate = "Services.Create";
    public const string ServicesUpdate = "Services.Update";
    public const string ServicesActivate = "Services.Activate";
    public const string ServicesDeactivate = "Services.Deactivate";

    public const string PackagesView = "Packages.View";
    public const string PackagesCreate = "Packages.Create";
    public const string PackagesUpdate = "Packages.Update";
    public const string PackagesActivate = "Packages.Activate";
    public const string PackagesDeactivate = "Packages.Deactivate";

    public const string FinanceInvoicesView = "Finance.Invoices.View";
    public const string FinanceInvoicesCreate = "Finance.Invoices.Create";
    public const string FinanceInvoicesUpdate = "Finance.Invoices.Update";
    public const string FinanceInvoicesIssue = "Finance.Invoices.Issue";
    public const string FinanceInvoicesVoid = "Finance.Invoices.Void";
    public const string FinancePaymentsView = "Finance.Payments.View";
    public const string FinancePaymentsCreate = "Finance.Payments.Create";
    public const string FinancePaymentsReverse = "Finance.Payments.Reverse";

    public const string FinanceAccountsView = "Finance.Accounts.View";
    public const string FinanceAccountsCreate = "Finance.Accounts.Create";
    public const string FinanceAccountsUpdate = "Finance.Accounts.Update";
    public const string FinanceAccountsActivate = "Finance.Accounts.Activate";
    public const string FinanceAccountsDeactivate = "Finance.Accounts.Deactivate";
    public const string FinanceFiscalYearsView = "Finance.FiscalYears.View";
    public const string FinanceFiscalYearsCreate = "Finance.FiscalYears.Create";
    public const string FinanceFiscalYearsUpdate = "Finance.FiscalYears.Update";
    public const string FinanceFiscalYearsClose = "Finance.FiscalYears.Close";
    public const string FinanceFiscalPeriodsView = "Finance.FiscalPeriods.View";
    public const string FinanceFiscalPeriodsCreate = "Finance.FiscalPeriods.Create";
    public const string FinanceFiscalPeriodsUpdate = "Finance.FiscalPeriods.Update";
    public const string FinanceFiscalPeriodsClose = "Finance.FiscalPeriods.Close";
    public const string FinanceJournalsView = "Finance.Journals.View";
    public const string FinanceJournalsCreate = "Finance.Journals.Create";
    public const string FinanceJournalsUpdate = "Finance.Journals.Update";
    public const string FinanceJournalsPost = "Finance.Journals.Post";
    public const string FinanceJournalsReverse = "Finance.Journals.Reverse";
    public const string FinanceGeneralLedgerView = "Finance.GeneralLedger.View";
    public const string FinanceTrialBalanceView = "Finance.TrialBalance.View";
    public const string FinanceArView = "Finance.AR.View";
    public const string FinanceArStatement = "Finance.AR.Statement";
    public const string FinanceApView = "Finance.AP.View";
    public const string FinanceApStatement = "Finance.AP.Statement";
    public const string FinanceAgingView = "Finance.Aging.View";

    public const string InventoryWarehousesView = "Inventory.Warehouses.View";
    public const string InventoryWarehousesCreate = "Inventory.Warehouses.Create";
    public const string InventoryWarehousesUpdate = "Inventory.Warehouses.Update";
    public const string InventoryWarehousesActivate = "Inventory.Warehouses.Activate";
    public const string InventoryWarehousesDeactivate = "Inventory.Warehouses.Deactivate";
    public const string InventoryCategoriesView = "Inventory.Categories.View";
    public const string InventoryCategoriesCreate = "Inventory.Categories.Create";
    public const string InventoryCategoriesUpdate = "Inventory.Categories.Update";
    public const string InventoryCategoriesActivate = "Inventory.Categories.Activate";
    public const string InventoryCategoriesDeactivate = "Inventory.Categories.Deactivate";
    public const string InventoryItemsView = "Inventory.Items.View";
    public const string InventoryItemsCreate = "Inventory.Items.Create";
    public const string InventoryItemsUpdate = "Inventory.Items.Update";
    public const string InventoryItemsActivate = "Inventory.Items.Activate";
    public const string InventoryItemsDeactivate = "Inventory.Items.Deactivate";
    public const string InventoryStockView = "Inventory.Stock.View";
    public const string InventoryStockAdjust = "Inventory.Stock.Adjust";
    public const string InventoryGoodsReceiptsView = "Inventory.GoodsReceipts.View";
    public const string InventoryGoodsReceiptsCreate = "Inventory.GoodsReceipts.Create";
    public const string InventoryGoodsReceiptsCancel = "Inventory.GoodsReceipts.Cancel";
    public const string InventoryValuationView = "Inventory.Valuation.View";
    public const string InventoryCostHistoryView = "Inventory.CostHistory.View";
    public const string InventoryCostLayersView = "Inventory.CostLayers.View";
    public const string InventoryIssueCostView = "Inventory.IssueCost.View";

    public const string ProcurementSuppliersView = "Procurement.Suppliers.View";
    public const string ProcurementSuppliersCreate = "Procurement.Suppliers.Create";
    public const string ProcurementSuppliersUpdate = "Procurement.Suppliers.Update";
    public const string ProcurementSuppliersActivate = "Procurement.Suppliers.Activate";
    public const string ProcurementSuppliersDeactivate = "Procurement.Suppliers.Deactivate";
    public const string ProcurementPurchaseOrdersView = "Procurement.PurchaseOrders.View";
    public const string ProcurementPurchaseOrdersCreate = "Procurement.PurchaseOrders.Create";
    public const string ProcurementPurchaseOrdersUpdate = "Procurement.PurchaseOrders.Update";
    public const string ProcurementPurchaseOrdersSubmit = "Procurement.PurchaseOrders.Submit";
    public const string ProcurementPurchaseOrdersApprove = "Procurement.PurchaseOrders.Approve";
    public const string ProcurementPurchaseOrdersCancel = "Procurement.PurchaseOrders.Cancel";

    public const string AssetsCategoriesView = "Assets.Categories.View";
    public const string AssetsCategoriesCreate = "Assets.Categories.Create";
    public const string AssetsCategoriesUpdate = "Assets.Categories.Update";
    public const string AssetsCategoriesActivate = "Assets.Categories.Activate";
    public const string AssetsCategoriesDeactivate = "Assets.Categories.Deactivate";
    public const string AssetsLocationsView = "Assets.Locations.View";
    public const string AssetsLocationsCreate = "Assets.Locations.Create";
    public const string AssetsLocationsUpdate = "Assets.Locations.Update";
    public const string AssetsLocationsActivate = "Assets.Locations.Activate";
    public const string AssetsLocationsDeactivate = "Assets.Locations.Deactivate";
    public const string AssetsView = "Assets.View";
    public const string AssetsCreate = "Assets.Create";
    public const string AssetsUpdate = "Assets.Update";
    public const string AssetsAssign = "Assets.Assign";
    public const string AssetsMaintenance = "Assets.Maintenance";
    public const string AssetsRetire = "Assets.Retire";
    public const string AssetsHistoryView = "Assets.History.View";
    public const string AssetsAccountingView = "Assets.Accounting.View";
    public const string AssetsAccountingCapitalize = "Assets.Accounting.Capitalize";
    public const string AssetsDepreciationView = "Assets.Depreciation.View";
    public const string AssetsDepreciationPost = "Assets.Depreciation.Post";
    public const string AssetsDepreciationSchedule = "Assets.Depreciation.Schedule";
    public const string AssetsDisposalView = "Assets.Disposal.View";
    public const string AssetsDisposalProcess = "Assets.Disposal.Process";

    public const string ReportsFinanceGeneralLedgerView = "Reports.Finance.GeneralLedger.View";
    public const string ReportsFinanceTrialBalanceView = "Reports.Finance.TrialBalance.View";
    public const string ReportsFinanceJournalsView = "Reports.Finance.Journals.View";
    public const string ReportsArStatementView = "Reports.AR.Statement.View";
    public const string ReportsArAgingView = "Reports.AR.Aging.View";
    public const string ReportsApStatementView = "Reports.AP.Statement.View";
    public const string ReportsApAgingView = "Reports.AP.Aging.View";
    public const string ReportsBillingInvoicesView = "Reports.Billing.Invoices.View";
    public const string ReportsBillingPaymentsView = "Reports.Billing.Payments.View";
    public const string ReportsBillingOutstandingView = "Reports.Billing.Outstanding.View";
    public const string ReportsInventoryStockView = "Reports.Inventory.Stock.View";
    public const string ReportsInventoryMovementsView = "Reports.Inventory.Movements.View";
    public const string ReportsInventoryValuationView = "Reports.Inventory.Valuation.View";
    public const string ReportsInventoryCogsView = "Reports.Inventory.COGS.View";
    public const string ReportsAssetsRegisterView = "Reports.Assets.Register.View";
    public const string ReportsAssetsDepreciationView = "Reports.Assets.Depreciation.View";
    public const string ReportsAssetsValuationView = "Reports.Assets.Valuation.View";
    public const string ReportsProcurementPurchaseOrdersView = "Reports.Procurement.PurchaseOrders.View";
    public const string ReportsProcurementReceivingView = "Reports.Procurement.Receiving.View";
    public const string ReportsManagementSummaryView = "Reports.Management.Summary.View";

    // Laser Clinic
    public const string LaserCustomersView = "LaserClinic.Customers.View";
    public const string LaserCustomersCreate = "LaserClinic.Customers.Create";
    public const string LaserCustomersUpdate = "LaserClinic.Customers.Update";
    public const string LaserServicesView = "LaserClinic.Services.View";
    public const string LaserServicesManage = "LaserClinic.Services.Manage";
    public const string LaserOffersView = "LaserClinic.Offers.View";
    public const string LaserOffersManage = "LaserClinic.Offers.Manage";
    public const string LaserAppointmentsView = "LaserClinic.Appointments.View";
    public const string LaserAppointmentsCreate = "LaserClinic.Appointments.Create";
    public const string LaserAppointmentsUpdate = "LaserClinic.Appointments.Update";
    public const string LaserAppointmentsCancel = "LaserClinic.Appointments.Cancel";
    public const string LaserSettingsView = "LaserClinic.Settings.View";
    public const string LaserSettingsManage = "LaserClinic.Settings.Manage";
    public const string LaserDashboardView = "LaserClinic.Dashboard.View";
    public const string LaserReportsView = "LaserClinic.Reports.View";
}
