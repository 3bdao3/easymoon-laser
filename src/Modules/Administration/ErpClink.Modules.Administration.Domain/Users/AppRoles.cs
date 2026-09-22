namespace ErpClink.Modules.Administration.Domain.Users;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Receptionist = "Receptionist";
    public const string Doctor = "Doctor";
    public const string Nurse = "Nurse";
    public const string Accountant = "Accountant";
    public const string Pharmacist = "Pharmacist";
    public const string InventoryManager = "InventoryManager";
    public const string ProcurementManager = "ProcurementManager";

    public static IReadOnlyList<string> All { get; } =
    [
        SuperAdmin,
        Admin,
        Receptionist,
        Doctor,
        Nurse,
        Accountant,
        Pharmacist,
        InventoryManager,
        ProcurementManager
    ];
}
