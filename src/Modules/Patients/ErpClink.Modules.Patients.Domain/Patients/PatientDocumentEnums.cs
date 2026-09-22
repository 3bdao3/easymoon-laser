namespace ErpClink.Modules.Patients.Domain.Patients;

public enum PatientDocumentCategory
{
    MedicalImaging = 1,
    Laboratory = 2,
    MedicalReports = 3,
    Prescriptions = 4,
    Other = 5
}

public enum PatientDocumentType
{
    XRay = 1,
    Ct = 2,
    Mri = 3,
    Ultrasound = 4,
    OtherImaging = 5,

    BloodTest = 10,
    UrineTest = 11,
    Pathology = 12,
    OtherLaboratory = 13,

    MedicalReport = 20,
    HospitalReport = 21,
    DischargeReport = 22,
    ReferralReport = 23,

    ExternalPrescription = 30,
    PreviousPrescription = 31,

    InsuranceDocument = 40,
    PatientDocument = 41,
    Other = 99
}

public static class PatientDocumentTypeRules
{
    public static PatientDocumentCategory GetCategory(PatientDocumentType type) => type switch
    {
        PatientDocumentType.XRay or PatientDocumentType.Ct or PatientDocumentType.Mri
            or PatientDocumentType.Ultrasound or PatientDocumentType.OtherImaging
            => PatientDocumentCategory.MedicalImaging,

        PatientDocumentType.BloodTest or PatientDocumentType.UrineTest
            or PatientDocumentType.Pathology or PatientDocumentType.OtherLaboratory
            => PatientDocumentCategory.Laboratory,

        PatientDocumentType.MedicalReport or PatientDocumentType.HospitalReport
            or PatientDocumentType.DischargeReport or PatientDocumentType.ReferralReport
            => PatientDocumentCategory.MedicalReports,

        PatientDocumentType.ExternalPrescription or PatientDocumentType.PreviousPrescription
            => PatientDocumentCategory.Prescriptions,

        _ => PatientDocumentCategory.Other
    };

    public static bool BelongsToCategory(PatientDocumentType type, PatientDocumentCategory category) =>
        GetCategory(type) == category;
}
