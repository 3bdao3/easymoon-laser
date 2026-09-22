namespace ErpClink.Modules.Patients.Infrastructure.Persistence;

/// <summary>
/// Per-organization sequence used by the temporary patient-number generator.
/// Format is configurable; default: P-{yyyy}-{000001}.
/// </summary>
public sealed class PatientNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}
