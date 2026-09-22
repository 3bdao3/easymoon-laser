namespace ErpClink.Modules.Patients.Domain.Patients;

/// <summary>
/// Clinical severity for allergies. Documented for V1; values may expand later.
/// </summary>
public enum AllergySeverity
{
    Unknown = 0,
    Mild = 1,
    Moderate = 2,
    Severe = 3,
    LifeThreatening = 4
}
