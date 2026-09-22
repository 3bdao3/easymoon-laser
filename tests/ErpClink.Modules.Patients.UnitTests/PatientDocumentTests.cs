using ErpClink.Modules.Patients.Domain.Patients;
using FluentAssertions;

namespace ErpClink.Modules.Patients.UnitTests;

public sealed class PatientDocumentTests
{
    [Fact]
    public void Create_rejects_type_outside_category()
    {
        var act = () => PatientDocument.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PatientDocumentCategory.Laboratory,
            PatientDocumentType.XRay,
            "xray.pdf",
            "org/patient/doc.pdf",
            "pdf",
            "application/pdf",
            100,
            null,
            null,
            null,
            "user",
            DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deactivate_is_idempotent()
    {
        var doc = PatientDocument.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PatientDocumentCategory.MedicalImaging,
            PatientDocumentType.XRay,
            "chest.pdf",
            "org/patient/doc.pdf",
            "pdf",
            "application/pdf",
            2048,
            new DateOnly(2026, 9, 14),
            "External",
            null,
            "user",
            DateTime.UtcNow);

        doc.Deactivate("user", DateTime.UtcNow);
        doc.IsActive.Should().BeFalse();
        doc.Deactivate("user", DateTime.UtcNow.AddMinutes(1));
        doc.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateMetadata_fails_when_inactive()
    {
        var doc = PatientDocument.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PatientDocumentCategory.Other,
            PatientDocumentType.Other,
            "note.pdf",
            "org/patient/doc.pdf",
            "pdf",
            "application/pdf",
            100,
            null,
            null,
            null,
            "user",
            DateTime.UtcNow);

        doc.Deactivate("user", DateTime.UtcNow);

        var act = () => doc.UpdateMetadata(
            PatientDocumentCategory.Other,
            PatientDocumentType.PatientDocument,
            null,
            "updated",
            null,
            "user",
            DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }
}
