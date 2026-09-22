using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Administration.Application.Options;
using ErpClink.Modules.Administration.Domain.Permissions;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Administration.Infrastructure.Identity;
using ErpClink.Modules.Administration.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErpClink.Modules.Administration.Infrastructure.Persistence;

public static class AdministrationDbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdministrationDbSeeder");
        var db = scope.ServiceProvider.GetRequiredService<AdministrationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminOptions = scope.ServiceProvider.GetRequiredService<IOptions<InitialAdminOptions>>().Value;
        var receptionistOptions = scope.ServiceProvider.GetRequiredService<IOptions<InitialReceptionistOptions>>().Value;

        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        await SeedRolesAsync(roleManager, cancellationToken);
        await SeedPermissionsAsync(db, cancellationToken);
        await SeedRolePermissionsAsync(db, roleManager, cancellationToken);
        await SeedSuperAdminAsync(userManager, roleManager, adminOptions, logger, cancellationToken);
        await SeedReceptionistUserAsync(userManager, roleManager, receptionistOptions, logger, cancellationToken);
    }

    private static readonly Dictionary<string, string> RoleDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        [AppRoles.SuperAdmin] = "System super administrator",
        [AppRoles.Admin] = "Clinic administrator",
        [AppRoles.Receptionist] = "موظفة السوشيال ميديا",
        [AppRoles.Doctor] = "Doctor",
        [AppRoles.Nurse] = "Nurse",
        [AppRoles.Accountant] = "Accountant",
        [AppRoles.Pharmacist] = "Pharmacist",
        [AppRoles.InventoryManager] = "Inventory manager",
        [AppRoles.ProcurementManager] = "Procurement manager"
    };

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, CancellationToken cancellationToken)
    {
        foreach (var roleName in AppRoles.All)
        {
            var description = RoleDescriptions.TryGetValue(roleName, out var desc)
                ? desc
                : $"{roleName} role";

            var existing = await roleManager.FindByNameAsync(roleName);
            if (existing is null)
            {
                var role = new ApplicationRole(roleName)
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    Description = description
                };

                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to seed role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                continue;
            }

            if (!string.Equals(existing.Description, description, StringComparison.Ordinal))
            {
                existing.Description = description;
                await roleManager.UpdateAsync(existing);
            }
        }
    }

    private static async Task SeedPermissionsAsync(AdministrationDbContext db, CancellationToken cancellationToken)
    {
        var definitions = GetPermissionDefinitions();
        var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync(cancellationToken);
        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var def in definitions)
        {
            if (existingSet.Contains(def.Code))
            {
                continue;
            }

            db.Permissions.Add(Permission.Create(def.Code, def.Name, def.Module, def.Description, "system", DateTime.UtcNow));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolePermissionsAsync(
        AdministrationDbContext db,
        RoleManager<ApplicationRole> roleManager,
        CancellationToken cancellationToken)
    {
        var permissions = await db.Permissions.AsNoTracking().ToListAsync(cancellationToken);
        var byCode = permissions.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

        var rolePermissionMap = BuildRolePermissionMap();

        foreach (var (roleName, permissionCodes) in rolePermissionMap)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var desiredIds = new HashSet<Guid>();
            foreach (var code in permissionCodes)
            {
                if (byCode.TryGetValue(code, out var permission))
                {
                    desiredIds.Add(permission.Id);
                }
            }

            var existingLinks = await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .ToListAsync(cancellationToken);

            var existingIds = existingLinks.Select(rp => rp.PermissionId).ToHashSet();

            foreach (var permissionId in desiredIds)
            {
                if (existingIds.Contains(permissionId))
                {
                    continue;
                }

                db.RolePermissions.Add(RolePermission.Create(role.Id, permissionId, DateTime.UtcNow));
            }

            // Align mapped roles with the seed map (remove extras).
            foreach (var link in existingLinks)
            {
                if (desiredIds.Contains(link.PermissionId))
                {
                    continue;
                }

                db.RolePermissions.Remove(link);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSuperAdminAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        InitialAdminOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning(
                "Initial SuperAdmin was not seeded because Authentication:InitialAdmin Email/Password are not configured.");
            return;
        }

        var user = await userManager.FindByEmailAsync(options.Email.Trim());
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = options.Email.Trim(),
                Email = options.Email.Trim(),
                EmailConfirmed = true,
                FullName = string.IsNullOrWhiteSpace(options.FullName) ? "System Super Admin" : options.FullName.Trim(),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = "system"
            };

            var createResult = await userManager.CreateAsync(user, options.Password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed SuperAdmin: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await roleManager.RoleExistsAsync(AppRoles.SuperAdmin))
        {
            throw new InvalidOperationException("SuperAdmin role was not seeded.");
        }

        if (!await userManager.IsInRoleAsync(user, AppRoles.SuperAdmin))
        {
            var roleResult = await userManager.AddToRoleAsync(user, AppRoles.SuperAdmin);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign SuperAdmin role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }
    }

    private static async Task SeedReceptionistUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        InitialReceptionistOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Password)
            || (string.IsNullOrWhiteSpace(options.UserName) && string.IsNullOrWhiteSpace(options.Email)))
        {
            logger.LogWarning(
                "Initial Receptionist was not seeded because Authentication:InitialReceptionist credentials are not configured.");
            return;
        }

        var userName = string.IsNullOrWhiteSpace(options.UserName)
            ? options.Email.Trim()
            : options.UserName.Trim();
        var email = string.IsNullOrWhiteSpace(options.Email)
            ? $"{userName}@easymoon.local"
            : options.Email.Trim();

        var user = await userManager.FindByNameAsync(userName)
                   ?? await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                FullName = string.IsNullOrWhiteSpace(options.FullName)
                    ? "موظفة السوشيال ميديا"
                    : options.FullName.Trim(),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = "system"
            };

            var createResult = await userManager.CreateAsync(user, options.Password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed Receptionist: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            var dirty = false;
            if (!string.Equals(user.UserName, userName, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = userName;
                dirty = true;
            }

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                user.EmailConfirmed = true;
                dirty = true;
            }

            if (!string.IsNullOrWhiteSpace(options.FullName)
                && !string.Equals(user.FullName, options.FullName.Trim(), StringComparison.Ordinal))
            {
                user.FullName = options.FullName.Trim();
                dirty = true;
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                dirty = true;
            }

            if (dirty)
            {
                user.UpdatedAtUtc = DateTime.UtcNow;
                user.UpdatedBy = "system";
                await userManager.UpdateAsync(user);
            }
        }

        if (!await roleManager.RoleExistsAsync(AppRoles.Receptionist))
        {
            throw new InvalidOperationException("Receptionist role was not seeded.");
        }

        if (!await userManager.IsInRoleAsync(user, AppRoles.Receptionist))
        {
            var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Receptionist);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign Receptionist role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }
    }

    private static IReadOnlyList<(string Code, string Name, string Module, string Description)> GetPermissionDefinitions() =>
    [
        (PermissionCodes.AdministrationUsersView, "View users", "Administration", "View administration users"),
        (PermissionCodes.AdministrationUsersCreate, "Create users", "Administration", "Create administration users"),
        (PermissionCodes.AdministrationUsersUpdate, "Update users", "Administration", "Update administration users"),
        (PermissionCodes.AdministrationUsersDelete, "Delete users", "Administration", "Deactivate/delete administration users"),
        (PermissionCodes.AdministrationRolesManage, "Manage roles", "Administration", "Manage roles and assignments"),
        (PermissionCodes.AdministrationPermissionsView, "View permissions", "Administration", "View permission catalog"),

        (PermissionCodes.PatientsView, "View patients", "Patients", "View patients"),
        (PermissionCodes.PatientsCreate, "Create patients", "Patients", "Create patients"),
        (PermissionCodes.PatientsUpdate, "Update patients", "Patients", "Update patients"),
        (PermissionCodes.PatientsDelete, "Delete patients", "Patients", "Legacy delete/deactivate patients"),
        (PermissionCodes.PatientsActivate, "Activate patients", "Patients", "Activate patients"),
        (PermissionCodes.PatientsDeactivate, "Deactivate patients", "Patients", "Deactivate patients"),
        (PermissionCodes.PatientsAllergiesView, "View patient allergies", "Patients", "View patient allergies"),
        (PermissionCodes.PatientsAllergiesManage, "Manage patient allergies", "Patients", "Manage patient allergies"),
        (PermissionCodes.PatientsDocumentsView, "View patient documents", "Patients", "View and download patient medical documents"),
        (PermissionCodes.PatientsDocumentsManage, "Manage patient documents", "Patients", "Upload, edit, and deactivate patient medical documents"),

        (PermissionCodes.DoctorsView, "View doctors", "Doctors", "View doctors"),
        (PermissionCodes.DoctorsCreate, "Create doctors", "Doctors", "Create doctors"),
        (PermissionCodes.DoctorsUpdate, "Update doctors", "Doctors", "Update doctors"),
        (PermissionCodes.DoctorsActivate, "Activate doctors", "Doctors", "Activate doctors"),
        (PermissionCodes.DoctorsDeactivate, "Deactivate doctors", "Doctors", "Deactivate doctors"),
        (PermissionCodes.DoctorsAssignClinic, "Assign doctor to clinic", "Doctors", "Assign/remove doctors from clinics"),

        (PermissionCodes.ClinicsView, "View clinics", "Clinics", "View clinics"),
        (PermissionCodes.ClinicsCreate, "Create clinics", "Clinics", "Create clinics"),
        (PermissionCodes.ClinicsUpdate, "Update clinics", "Clinics", "Update clinics"),
        (PermissionCodes.ClinicsActivate, "Activate clinics", "Clinics", "Activate clinics"),
        (PermissionCodes.ClinicsDeactivate, "Deactivate clinics", "Clinics", "Deactivate clinics"),

        (PermissionCodes.SpecialtiesView, "View specialties", "Specialties", "View medical specialties"),
        (PermissionCodes.SpecialtiesManage, "Manage specialties", "Specialties", "Create/update/activate specialties"),

        (PermissionCodes.SchedulingView, "View schedules", "Scheduling", "View doctor working schedules"),
        (PermissionCodes.SchedulingCreate, "Create schedules", "Scheduling", "Create doctor working schedules"),
        (PermissionCodes.SchedulingUpdate, "Update schedules", "Scheduling", "Update doctor working schedules"),
        (PermissionCodes.SchedulingActivate, "Activate schedules", "Scheduling", "Activate doctor working schedules"),
        (PermissionCodes.SchedulingDeactivate, "Deactivate schedules", "Scheduling", "Deactivate doctor working schedules"),
        (PermissionCodes.SchedulingAvailabilityView, "View availability", "Scheduling", "View calculated doctor availability slots"),

        (PermissionCodes.AppointmentsView, "View appointments", "Appointments", "View appointments"),
        (PermissionCodes.AppointmentsCreate, "Create appointments", "Appointments", "Create appointments"),
        (PermissionCodes.AppointmentsUpdate, "Update appointments", "Appointments", "Update appointments"),
        (PermissionCodes.AppointmentsConfirm, "Confirm appointments", "Appointments", "Confirm appointments"),
        (PermissionCodes.AppointmentsCancel, "Cancel appointments", "Appointments", "Cancel appointments"),
        (PermissionCodes.AppointmentsReschedule, "Reschedule appointments", "Appointments", "Reschedule appointments"),
        (PermissionCodes.AppointmentsNoShow, "Mark appointment no-show", "Appointments", "Mark appointments as no-show"),

        (PermissionCodes.QueueView, "View queue", "Queue", "View queue entries"),
        (PermissionCodes.QueueCheckIn, "Check-in patients", "Queue", "Check-in appointments into queue"),
        (PermissionCodes.QueueCall, "Call queue", "Queue", "Call waiting patients"),
        (PermissionCodes.QueueStartService, "Start queue service", "Queue", "Start service for called patients"),
        (PermissionCodes.QueueComplete, "Complete queue entry", "Queue", "Complete queue service stage"),
        (PermissionCodes.QueueSkip, "Skip queue entry", "Queue", "Skip queue entries"),
        (PermissionCodes.QueueCancel, "Cancel queue entry", "Queue", "Cancel queue entries"),

        (PermissionCodes.MedicalVisitsView, "View medical visits", "MedicalVisits", "View medical visits and patient clinical history"),
        (PermissionCodes.MedicalVisitsCreate, "Start medical visits", "MedicalVisits", "Start medical visits"),
        (PermissionCodes.MedicalVisitsUpdate, "Update clinical notes", "MedicalVisits", "Update medical visit clinical notes"),
        (PermissionCodes.MedicalVisitsComplete, "Complete medical visits", "MedicalVisits", "Complete medical visits"),
        (PermissionCodes.MedicalVisitsCancel, "Cancel medical visits", "MedicalVisits", "Cancel medical visits"),

        (PermissionCodes.PrescriptionsView, "View prescriptions", "Prescriptions", "View prescriptions"),
        (PermissionCodes.PrescriptionsCreate, "Create prescriptions", "Prescriptions", "Create draft prescriptions"),
        (PermissionCodes.PrescriptionsUpdate, "Update prescriptions", "Prescriptions", "Update draft prescriptions and items"),
        (PermissionCodes.PrescriptionsIssue, "Issue prescriptions", "Prescriptions", "Issue prescriptions"),
        (PermissionCodes.PrescriptionsCancel, "Cancel prescriptions", "Prescriptions", "Cancel prescriptions"),

        (PermissionCodes.MedicationsView, "View medications", "Medications", "View medication catalog"),
        (PermissionCodes.MedicationsCreate, "Create medications", "Medications", "Create medications"),
        (PermissionCodes.MedicationsUpdate, "Update medications", "Medications", "Update medications"),
        (PermissionCodes.MedicationsActivate, "Activate medications", "Medications", "Activate medications"),
        (PermissionCodes.MedicationsDeactivate, "Deactivate medications", "Medications", "Deactivate medications"),

        (PermissionCodes.ServicesView, "View services", "Services", "View healthcare service catalog"),
        (PermissionCodes.ServicesCreate, "Create services", "Services", "Create healthcare services and categories"),
        (PermissionCodes.ServicesUpdate, "Update services", "Services", "Update healthcare services and categories"),
        (PermissionCodes.ServicesActivate, "Activate services", "Services", "Activate healthcare services and categories"),
        (PermissionCodes.ServicesDeactivate, "Deactivate services", "Services", "Deactivate healthcare services and categories"),

        (PermissionCodes.PackagesView, "View packages", "Packages", "View service packages"),
        (PermissionCodes.PackagesCreate, "Create packages", "Packages", "Create service packages"),
        (PermissionCodes.PackagesUpdate, "Update packages", "Packages", "Update service packages and items"),
        (PermissionCodes.PackagesActivate, "Activate packages", "Packages", "Activate service packages"),
        (PermissionCodes.PackagesDeactivate, "Deactivate packages", "Packages", "Deactivate service packages"),

        (PermissionCodes.FinanceInvoicesView, "View invoices", "Finance", "View invoices"),
        (PermissionCodes.FinanceInvoicesCreate, "Create invoices", "Finance", "Create draft invoices"),
        (PermissionCodes.FinanceInvoicesUpdate, "Update invoices", "Finance", "Update draft invoices and lines"),
        (PermissionCodes.FinanceInvoicesIssue, "Issue invoices", "Finance", "Issue draft invoices"),
        (PermissionCodes.FinanceInvoicesVoid, "Void invoices", "Finance", "Void unpaid invoices"),
        (PermissionCodes.FinancePaymentsView, "View payments", "Finance", "View payments"),
        (PermissionCodes.FinancePaymentsCreate, "Create payments", "Finance", "Record invoice payments"),
        (PermissionCodes.FinancePaymentsReverse, "Reverse payments", "Finance", "Reverse captured payments"),

        (PermissionCodes.FinanceAccountsView, "View GL accounts", "Finance", "View chart of accounts"),
        (PermissionCodes.FinanceAccountsCreate, "Create GL accounts", "Finance", "Create chart of accounts entries"),
        (PermissionCodes.FinanceAccountsUpdate, "Update GL accounts", "Finance", "Update chart of accounts entries"),
        (PermissionCodes.FinanceAccountsActivate, "Activate GL accounts", "Finance", "Activate GL accounts"),
        (PermissionCodes.FinanceAccountsDeactivate, "Deactivate GL accounts", "Finance", "Deactivate GL accounts"),
        (PermissionCodes.FinanceFiscalYearsView, "View fiscal years", "Finance", "View fiscal years"),
        (PermissionCodes.FinanceFiscalYearsCreate, "Create fiscal years", "Finance", "Create fiscal years"),
        (PermissionCodes.FinanceFiscalYearsUpdate, "Update fiscal years", "Finance", "Update fiscal years"),
        (PermissionCodes.FinanceFiscalYearsClose, "Close fiscal years", "Finance", "Close fiscal years"),
        (PermissionCodes.FinanceFiscalPeriodsView, "View fiscal periods", "Finance", "View fiscal periods"),
        (PermissionCodes.FinanceFiscalPeriodsCreate, "Create fiscal periods", "Finance", "Create fiscal periods"),
        (PermissionCodes.FinanceFiscalPeriodsUpdate, "Update fiscal periods", "Finance", "Update fiscal periods"),
        (PermissionCodes.FinanceFiscalPeriodsClose, "Close fiscal periods", "Finance", "Close fiscal periods"),
        (PermissionCodes.FinanceJournalsView, "View journals", "Finance", "View manual journal entries"),
        (PermissionCodes.FinanceJournalsCreate, "Create journals", "Finance", "Create draft journal entries"),
        (PermissionCodes.FinanceJournalsUpdate, "Update journals", "Finance", "Update draft journal entries"),
        (PermissionCodes.FinanceJournalsPost, "Post journals", "Finance", "Post balanced journal entries"),
        (PermissionCodes.FinanceJournalsReverse, "Reverse journals", "Finance", "Reverse posted journal entries"),
        (PermissionCodes.FinanceGeneralLedgerView, "View general ledger", "Finance", "Query posted GL activity"),
        (PermissionCodes.FinanceTrialBalanceView, "View trial balance", "Finance", "View trial balance report"),
        (PermissionCodes.FinanceArView, "View AR subledger", "Finance", "View accounts receivable transactions and balances"),
        (PermissionCodes.FinanceArStatement, "View AR statements", "Finance", "View customer AR statements"),
        (PermissionCodes.FinanceApView, "View AP subledger", "Finance", "View accounts payable transactions and balances"),
        (PermissionCodes.FinanceApStatement, "View AP statements", "Finance", "View supplier AP statements"),
        (PermissionCodes.FinanceAgingView, "View AR/AP aging", "Finance", "View AR and AP aging"),

        (PermissionCodes.InventoryWarehousesView, "View warehouses", "Inventory", "View inventory warehouses"),
        (PermissionCodes.InventoryWarehousesCreate, "Create warehouses", "Inventory", "Create inventory warehouses"),
        (PermissionCodes.InventoryWarehousesUpdate, "Update warehouses", "Inventory", "Update inventory warehouses"),
        (PermissionCodes.InventoryWarehousesActivate, "Activate warehouses", "Inventory", "Activate inventory warehouses"),
        (PermissionCodes.InventoryWarehousesDeactivate, "Deactivate warehouses", "Inventory", "Deactivate inventory warehouses"),
        (PermissionCodes.InventoryCategoriesView, "View inventory categories", "Inventory", "View inventory categories"),
        (PermissionCodes.InventoryCategoriesCreate, "Create inventory categories", "Inventory", "Create inventory categories"),
        (PermissionCodes.InventoryCategoriesUpdate, "Update inventory categories", "Inventory", "Update inventory categories"),
        (PermissionCodes.InventoryCategoriesActivate, "Activate inventory categories", "Inventory", "Activate inventory categories"),
        (PermissionCodes.InventoryCategoriesDeactivate, "Deactivate inventory categories", "Inventory", "Deactivate inventory categories"),
        (PermissionCodes.InventoryItemsView, "View inventory items", "Inventory", "View inventory items"),
        (PermissionCodes.InventoryItemsCreate, "Create inventory items", "Inventory", "Create inventory items"),
        (PermissionCodes.InventoryItemsUpdate, "Update inventory items", "Inventory", "Update inventory items"),
        (PermissionCodes.InventoryItemsActivate, "Activate inventory items", "Inventory", "Activate inventory items"),
        (PermissionCodes.InventoryItemsDeactivate, "Deactivate inventory items", "Inventory", "Deactivate inventory items"),
        (PermissionCodes.InventoryStockView, "View stock", "Inventory", "View stock balances"),
        (PermissionCodes.InventoryStockAdjust, "Adjust stock", "Inventory", "Adjust inventory stock"),
        (PermissionCodes.InventoryGoodsReceiptsView, "View goods receipts", "Inventory", "View goods receipts"),
        (PermissionCodes.InventoryGoodsReceiptsCreate, "Create goods receipts", "Inventory", "Receive purchase orders into stock"),
        (PermissionCodes.InventoryGoodsReceiptsCancel, "Cancel goods receipts", "Inventory", "Cancel posted goods receipts"),
        (PermissionCodes.InventoryValuationView, "View inventory valuation", "Inventory", "View inventory valuation and values"),
        (PermissionCodes.InventoryCostHistoryView, "View inventory cost history", "Inventory", "View item cost history"),
        (PermissionCodes.InventoryCostLayersView, "View inventory cost layers", "Inventory", "View FIFO cost layers"),
        (PermissionCodes.InventoryIssueCostView, "View inventory issue costs", "Inventory", "View cost of inventory issues"),

        (PermissionCodes.ProcurementSuppliersView, "View suppliers", "Procurement", "View suppliers"),
        (PermissionCodes.ProcurementSuppliersCreate, "Create suppliers", "Procurement", "Create suppliers"),
        (PermissionCodes.ProcurementSuppliersUpdate, "Update suppliers", "Procurement", "Update suppliers"),
        (PermissionCodes.ProcurementSuppliersActivate, "Activate suppliers", "Procurement", "Activate suppliers"),
        (PermissionCodes.ProcurementSuppliersDeactivate, "Deactivate suppliers", "Procurement", "Deactivate suppliers"),
        (PermissionCodes.ProcurementPurchaseOrdersView, "View purchase orders", "Procurement", "View purchase orders"),
        (PermissionCodes.ProcurementPurchaseOrdersCreate, "Create purchase orders", "Procurement", "Create draft purchase orders"),
        (PermissionCodes.ProcurementPurchaseOrdersUpdate, "Update purchase orders", "Procurement", "Update draft purchase orders and lines"),
        (PermissionCodes.ProcurementPurchaseOrdersSubmit, "Submit purchase orders", "Procurement", "Submit draft purchase orders"),
        (PermissionCodes.ProcurementPurchaseOrdersApprove, "Approve purchase orders", "Procurement", "Approve submitted purchase orders"),
        (PermissionCodes.ProcurementPurchaseOrdersCancel, "Cancel purchase orders", "Procurement", "Cancel purchase orders"),

        (PermissionCodes.AssetsCategoriesView, "View asset categories", "Assets", "View fixed asset categories"),
        (PermissionCodes.AssetsCategoriesCreate, "Create asset categories", "Assets", "Create fixed asset categories"),
        (PermissionCodes.AssetsCategoriesUpdate, "Update asset categories", "Assets", "Update fixed asset categories"),
        (PermissionCodes.AssetsCategoriesActivate, "Activate asset categories", "Assets", "Activate fixed asset categories"),
        (PermissionCodes.AssetsCategoriesDeactivate, "Deactivate asset categories", "Assets", "Deactivate fixed asset categories"),
        (PermissionCodes.AssetsLocationsView, "View asset locations", "Assets", "View fixed asset locations"),
        (PermissionCodes.AssetsLocationsCreate, "Create asset locations", "Assets", "Create fixed asset locations"),
        (PermissionCodes.AssetsLocationsUpdate, "Update asset locations", "Assets", "Update fixed asset locations"),
        (PermissionCodes.AssetsLocationsActivate, "Activate asset locations", "Assets", "Activate fixed asset locations"),
        (PermissionCodes.AssetsLocationsDeactivate, "Deactivate asset locations", "Assets", "Deactivate fixed asset locations"),
        (PermissionCodes.AssetsView, "View assets", "Assets", "View fixed assets"),
        (PermissionCodes.AssetsCreate, "Create assets", "Assets", "Register fixed assets"),
        (PermissionCodes.AssetsUpdate, "Update assets", "Assets", "Update fixed asset details"),
        (PermissionCodes.AssetsAssign, "Assign assets", "Assets", "Change asset location"),
        (PermissionCodes.AssetsMaintenance, "Maintain assets", "Assets", "Start and complete asset maintenance"),
        (PermissionCodes.AssetsRetire, "Retire assets", "Assets", "Retire or dispose fixed assets"),
        (PermissionCodes.AssetsHistoryView, "View asset history", "Assets", "View immutable asset history"),
        (PermissionCodes.AssetsAccountingView, "View asset accounting", "Assets", "View asset financial profiles and valuation"),
        (PermissionCodes.AssetsAccountingCapitalize, "Capitalize assets", "Assets", "Capitalize fixed assets for depreciation"),
        (PermissionCodes.AssetsDepreciationView, "View depreciation", "Assets", "View depreciation transactions"),
        (PermissionCodes.AssetsDepreciationPost, "Post depreciation", "Assets", "Post asset depreciation periods"),
        (PermissionCodes.AssetsDepreciationSchedule, "View depreciation schedule", "Assets", "View depreciation schedules"),
        (PermissionCodes.AssetsDisposalView, "View asset disposals", "Assets", "View financial disposals"),
        (PermissionCodes.AssetsDisposalProcess, "Process asset disposals", "Assets", "Record financial disposal facts"),
        (PermissionCodes.ReportsFinanceGeneralLedgerView, "View GL report", "Reports", "General ledger report"),
        (PermissionCodes.ReportsFinanceTrialBalanceView, "View trial balance report", "Reports", "Trial balance report"),
        (PermissionCodes.ReportsFinanceJournalsView, "View journal register report", "Reports", "Journal register report"),
        (PermissionCodes.ReportsArStatementView, "View AR statement report", "Reports", "Accounts receivable statement"),
        (PermissionCodes.ReportsArAgingView, "View AR aging report", "Reports", "Accounts receivable aging"),
        (PermissionCodes.ReportsApStatementView, "View AP statement report", "Reports", "Accounts payable statement"),
        (PermissionCodes.ReportsApAgingView, "View AP aging report", "Reports", "Accounts payable aging"),
        (PermissionCodes.ReportsBillingInvoicesView, "View invoice register report", "Reports", "Billing invoice register"),
        (PermissionCodes.ReportsBillingPaymentsView, "View payment register report", "Reports", "Billing payment register"),
        (PermissionCodes.ReportsBillingOutstandingView, "View outstanding invoices report", "Reports", "Outstanding billing invoices"),
        (PermissionCodes.ReportsInventoryStockView, "View stock balance report", "Reports", "Inventory stock balances"),
        (PermissionCodes.ReportsInventoryMovementsView, "View stock movement report", "Reports", "Inventory stock movements"),
        (PermissionCodes.ReportsInventoryValuationView, "View inventory valuation report", "Reports", "Inventory valuation"),
        (PermissionCodes.ReportsInventoryCogsView, "View inventory COGS report", "Reports", "Inventory issue-cost / COGS foundation"),
        (PermissionCodes.ReportsAssetsRegisterView, "View asset register report", "Reports", "Asset financial register"),
        (PermissionCodes.ReportsAssetsDepreciationView, "View depreciation report", "Reports", "Asset depreciation"),
        (PermissionCodes.ReportsAssetsValuationView, "View asset valuation report", "Reports", "Asset valuation / NBV"),
        (PermissionCodes.ReportsProcurementPurchaseOrdersView, "View PO register report", "Reports", "Purchase order register (not AP)"),
        (PermissionCodes.ReportsProcurementReceivingView, "View receiving summary report", "Reports", "Goods receiving summary"),
        (PermissionCodes.ReportsManagementSummaryView, "View management summary", "Reports", "Management KPI summary"),
        (PermissionCodes.LaserCustomersView, "View laser customers", "LaserClinic", "View customers"),
        (PermissionCodes.LaserCustomersCreate, "Create laser customers", "LaserClinic", "Create customers"),
        (PermissionCodes.LaserCustomersUpdate, "Update laser customers", "LaserClinic", "Update customers"),
        (PermissionCodes.LaserServicesView, "View laser services", "LaserClinic", "View laser services"),
        (PermissionCodes.LaserServicesManage, "Manage laser services", "LaserClinic", "Create/update laser services"),
        (PermissionCodes.LaserOffersView, "View laser offers", "LaserClinic", "View clinic offers"),
        (PermissionCodes.LaserOffersManage, "Manage laser offers", "LaserClinic", "Create/update clinic offers"),
        (PermissionCodes.LaserAppointmentsView, "View laser appointments", "LaserClinic", "View appointments"),
        (PermissionCodes.LaserAppointmentsCreate, "Create laser appointments", "LaserClinic", "Book appointments"),
        (PermissionCodes.LaserAppointmentsUpdate, "Update laser appointments", "LaserClinic", "Update appointment status"),
        (PermissionCodes.LaserAppointmentsCancel, "Cancel laser appointments", "LaserClinic", "Cancel appointments"),
        (PermissionCodes.LaserSettingsView, "View clinic settings", "LaserClinic", "View working hours"),
        (PermissionCodes.LaserSettingsManage, "Manage clinic settings", "LaserClinic", "Update working hours"),
        (PermissionCodes.LaserDashboardView, "View laser dashboard", "LaserClinic", "Dashboard KPIs"),
        (PermissionCodes.LaserReportsView, "View laser reports", "LaserClinic", "Basic reports")
    ];

    private static Dictionary<string, string[]> BuildRolePermissionMap()
    {
        var allPermissions = GetPermissionDefinitions().Select(p => p.Code).ToArray();

        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [AppRoles.SuperAdmin] = allPermissions,
            [AppRoles.Admin] = allPermissions,
            [AppRoles.Receptionist] =
            [
                // Easy Moon — Social Media employee (موظفة السوشيال ميديا)
                PermissionCodes.LaserCustomersView,
                PermissionCodes.LaserCustomersCreate,
                PermissionCodes.LaserCustomersUpdate,
                PermissionCodes.LaserServicesView, // required to select areas when booking (not manage)
                PermissionCodes.LaserOffersView, // view offers only (no manage)
                PermissionCodes.LaserAppointmentsView,
                PermissionCodes.LaserAppointmentsCreate,
                PermissionCodes.LaserAppointmentsUpdate,
                PermissionCodes.LaserAppointmentsCancel,
                PermissionCodes.LaserDashboardView
            ],
            [AppRoles.Doctor] =
            [
                PermissionCodes.PatientsView,
                PermissionCodes.PatientsAllergiesView,
                PermissionCodes.PatientsDocumentsView,
                PermissionCodes.PatientsDocumentsManage,
                PermissionCodes.AppointmentsView,
                PermissionCodes.AppointmentsUpdate,
                PermissionCodes.AppointmentsConfirm,
                PermissionCodes.AppointmentsNoShow,
                PermissionCodes.QueueView,
                PermissionCodes.QueueCall,
                PermissionCodes.QueueStartService,
                PermissionCodes.QueueComplete,
                PermissionCodes.QueueSkip,
                PermissionCodes.MedicalVisitsView,
                PermissionCodes.MedicalVisitsCreate,
                PermissionCodes.MedicalVisitsUpdate,
                PermissionCodes.MedicalVisitsComplete,
                PermissionCodes.MedicalVisitsCancel,
                PermissionCodes.PrescriptionsView,
                PermissionCodes.PrescriptionsCreate,
                PermissionCodes.PrescriptionsUpdate,
                PermissionCodes.PrescriptionsIssue,
                PermissionCodes.PrescriptionsCancel,
                PermissionCodes.MedicationsView,
                PermissionCodes.MedicationsCreate,
                PermissionCodes.MedicationsUpdate,
                PermissionCodes.MedicationsActivate,
                PermissionCodes.MedicationsDeactivate,
                PermissionCodes.DoctorsView,
                PermissionCodes.ClinicsView,
                PermissionCodes.SpecialtiesView,
                PermissionCodes.SchedulingView,
                PermissionCodes.SchedulingAvailabilityView
            ],
            [AppRoles.Nurse] =
            [
                PermissionCodes.PatientsView,
                PermissionCodes.PatientsAllergiesView,
                PermissionCodes.PatientsAllergiesManage,
                PermissionCodes.PatientsDocumentsView,
                PermissionCodes.PatientsDocumentsManage,
                PermissionCodes.AppointmentsView,
                PermissionCodes.QueueView,
                PermissionCodes.QueueCall,
                PermissionCodes.MedicalVisitsView,
                PermissionCodes.PrescriptionsView,
                PermissionCodes.MedicationsView,
                PermissionCodes.ServicesView,
                PermissionCodes.PackagesView,
                PermissionCodes.DoctorsView,
                PermissionCodes.ClinicsView,
                PermissionCodes.SpecialtiesView,
                PermissionCodes.SchedulingView,
                PermissionCodes.SchedulingAvailabilityView
            ],
            [AppRoles.Accountant] =
            [
                PermissionCodes.FinanceInvoicesView,
                PermissionCodes.FinanceInvoicesCreate,
                PermissionCodes.FinanceInvoicesUpdate,
                PermissionCodes.FinanceInvoicesIssue,
                PermissionCodes.FinanceInvoicesVoid,
                PermissionCodes.FinancePaymentsView,
                PermissionCodes.FinancePaymentsCreate,
                PermissionCodes.FinancePaymentsReverse,
                PermissionCodes.FinanceAccountsView,
                PermissionCodes.FinanceAccountsCreate,
                PermissionCodes.FinanceAccountsUpdate,
                PermissionCodes.FinanceAccountsActivate,
                PermissionCodes.FinanceAccountsDeactivate,
                PermissionCodes.FinanceFiscalYearsView,
                PermissionCodes.FinanceFiscalYearsCreate,
                PermissionCodes.FinanceFiscalYearsUpdate,
                PermissionCodes.FinanceFiscalYearsClose,
                PermissionCodes.FinanceFiscalPeriodsView,
                PermissionCodes.FinanceFiscalPeriodsCreate,
                PermissionCodes.FinanceFiscalPeriodsUpdate,
                PermissionCodes.FinanceFiscalPeriodsClose,
                PermissionCodes.FinanceJournalsView,
                PermissionCodes.FinanceJournalsCreate,
                PermissionCodes.FinanceJournalsUpdate,
                PermissionCodes.FinanceJournalsPost,
                PermissionCodes.FinanceJournalsReverse,
                PermissionCodes.FinanceGeneralLedgerView,
                PermissionCodes.FinanceTrialBalanceView,
                PermissionCodes.FinanceArView,
                PermissionCodes.FinanceArStatement,
                PermissionCodes.FinanceApView,
                PermissionCodes.FinanceApStatement,
                PermissionCodes.FinanceAgingView,
                PermissionCodes.PatientsView,
                PermissionCodes.ServicesView,
                PermissionCodes.PackagesView,
                PermissionCodes.MedicalVisitsView,
                PermissionCodes.ProcurementSuppliersView,
                PermissionCodes.ReportsFinanceGeneralLedgerView,
                PermissionCodes.ReportsFinanceTrialBalanceView,
                PermissionCodes.ReportsFinanceJournalsView,
                PermissionCodes.ReportsArStatementView,
                PermissionCodes.ReportsArAgingView,
                PermissionCodes.ReportsApStatementView,
                PermissionCodes.ReportsApAgingView,
                PermissionCodes.ReportsBillingInvoicesView,
                PermissionCodes.ReportsBillingPaymentsView,
                PermissionCodes.ReportsBillingOutstandingView,
                PermissionCodes.ReportsManagementSummaryView
            ],
            [AppRoles.Pharmacist] =
            [
                PermissionCodes.PatientsView,
                PermissionCodes.PatientsAllergiesView,
                PermissionCodes.PatientsDocumentsView,
                PermissionCodes.InventoryItemsView,
                PermissionCodes.InventoryStockView
            ],
            [AppRoles.InventoryManager] =
            [
                PermissionCodes.InventoryWarehousesView,
                PermissionCodes.InventoryWarehousesCreate,
                PermissionCodes.InventoryWarehousesUpdate,
                PermissionCodes.InventoryWarehousesActivate,
                PermissionCodes.InventoryWarehousesDeactivate,
                PermissionCodes.InventoryCategoriesView,
                PermissionCodes.InventoryCategoriesCreate,
                PermissionCodes.InventoryCategoriesUpdate,
                PermissionCodes.InventoryCategoriesActivate,
                PermissionCodes.InventoryCategoriesDeactivate,
                PermissionCodes.InventoryItemsView,
                PermissionCodes.InventoryItemsCreate,
                PermissionCodes.InventoryItemsUpdate,
                PermissionCodes.InventoryItemsActivate,
                PermissionCodes.InventoryItemsDeactivate,
                PermissionCodes.InventoryStockView,
                PermissionCodes.InventoryStockAdjust,
                PermissionCodes.InventoryGoodsReceiptsView,
                PermissionCodes.InventoryGoodsReceiptsCreate,
                PermissionCodes.InventoryGoodsReceiptsCancel,
                PermissionCodes.InventoryValuationView,
                PermissionCodes.InventoryCostHistoryView,
                PermissionCodes.InventoryCostLayersView,
                PermissionCodes.InventoryIssueCostView,
                PermissionCodes.ProcurementPurchaseOrdersView,
                PermissionCodes.ProcurementSuppliersView,
                PermissionCodes.AssetsCategoriesView,
                PermissionCodes.AssetsCategoriesCreate,
                PermissionCodes.AssetsCategoriesUpdate,
                PermissionCodes.AssetsCategoriesActivate,
                PermissionCodes.AssetsCategoriesDeactivate,
                PermissionCodes.AssetsLocationsView,
                PermissionCodes.AssetsLocationsCreate,
                PermissionCodes.AssetsLocationsUpdate,
                PermissionCodes.AssetsLocationsActivate,
                PermissionCodes.AssetsLocationsDeactivate,
                PermissionCodes.AssetsView,
                PermissionCodes.AssetsCreate,
                PermissionCodes.AssetsUpdate,
                PermissionCodes.AssetsAssign,
                PermissionCodes.AssetsMaintenance,
                PermissionCodes.AssetsRetire,
                PermissionCodes.AssetsHistoryView,
                PermissionCodes.AssetsAccountingView,
                PermissionCodes.AssetsAccountingCapitalize,
                PermissionCodes.AssetsDepreciationView,
                PermissionCodes.AssetsDepreciationPost,
                PermissionCodes.AssetsDepreciationSchedule,
                PermissionCodes.AssetsDisposalView,
                PermissionCodes.AssetsDisposalProcess,
                PermissionCodes.ReportsInventoryStockView,
                PermissionCodes.ReportsInventoryMovementsView,
                PermissionCodes.ReportsInventoryValuationView,
                PermissionCodes.ReportsInventoryCogsView,
                PermissionCodes.ReportsAssetsRegisterView,
                PermissionCodes.ReportsAssetsDepreciationView,
                PermissionCodes.ReportsAssetsValuationView,
                PermissionCodes.ReportsProcurementPurchaseOrdersView,
                PermissionCodes.ReportsProcurementReceivingView
            ],
            [AppRoles.ProcurementManager] =
            [
                PermissionCodes.InventoryItemsView,
                PermissionCodes.InventoryGoodsReceiptsView,
                PermissionCodes.ProcurementSuppliersView,
                PermissionCodes.ProcurementSuppliersCreate,
                PermissionCodes.ProcurementSuppliersUpdate,
                PermissionCodes.ProcurementSuppliersActivate,
                PermissionCodes.ProcurementSuppliersDeactivate,
                PermissionCodes.ProcurementPurchaseOrdersView,
                PermissionCodes.ProcurementPurchaseOrdersCreate,
                PermissionCodes.ProcurementPurchaseOrdersUpdate,
                PermissionCodes.ProcurementPurchaseOrdersSubmit,
                PermissionCodes.ProcurementPurchaseOrdersApprove,
                PermissionCodes.ProcurementPurchaseOrdersCancel,
                PermissionCodes.ReportsProcurementPurchaseOrdersView,
                PermissionCodes.ReportsProcurementReceivingView
            ]
        };
    }
}
