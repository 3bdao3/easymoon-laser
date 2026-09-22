using ErpClink.Modules.MedicalVisits.Application.Visits.Models;
using ErpClink.Modules.MedicalVisits.Domain.Visits;

namespace ErpClink.Modules.MedicalVisits.Application.Visits;

/// <summary>
/// Pure derivation of Patient 360 visit context — no persistence.
/// </summary>
public static class PatientVisitContextFactory
{
    public static PatientVisitContextDto FromVisits(IReadOnlyList<MedicalVisit> visits)
    {
        var ordered = visits
            .OrderByDescending(v => v.VisitDate)
            .ThenByDescending(v => v.CreatedAtUtc)
            .ToList();

        var nonCancelled = ordered.Where(v => v.Status != MedicalVisitStatus.Cancelled).ToList();
        var completed = nonCancelled.Where(v => v.Status == MedicalVisitStatus.Completed).ToList();
        var active = nonCancelled
            .Where(v => v.Status is MedicalVisitStatus.Open or MedicalVisitStatus.InProgress)
            .ToList();

        var total = nonCancelled.Count;
        // Returning = has at least one completed visit, or more than one non-cancelled encounter.
        var isReturning = completed.Count > 0 || nonCancelled.Count > 1;
        var relationship = isReturning
            ? PatientVisitContextCodes.RelationshipReturning
            : PatientVisitContextCodes.RelationshipNew;

        string treatmentState;
        if (active.Count > 0)
            treatmentState = PatientVisitContextCodes.TreatmentInProgress;
        else if (completed.Count > 0)
            treatmentState = PatientVisitContextCodes.TreatmentCompleted;
        else
            treatmentState = PatientVisitContextCodes.TreatmentNone;

        MedicalVisit? activeVisit = active
            .OrderByDescending(v => v.VisitDate)
            .ThenByDescending(v => v.CreatedAtUtc)
            .FirstOrDefault();

        MedicalVisit? lastVisit = nonCancelled.FirstOrDefault();

        string? encounterKind = null;
        var previousHadFollowUp = false;

        if (activeVisit is not null)
        {
            var priorCompleted = completed
                .Where(v => v.Id != activeVisit.Id)
                .OrderByDescending(v => v.VisitDate)
                .ThenByDescending(v => v.CreatedAtUtc)
                .ToList();

            previousHadFollowUp = priorCompleted.FirstOrDefault()?.FollowUpNotes is { Length: > 0 };

            if (priorCompleted.Count == 0)
                encounterKind = PatientVisitContextCodes.EncounterFirstVisit;
            else if (previousHadFollowUp)
                encounterKind = PatientVisitContextCodes.EncounterFollowUp;
            else
                encounterKind = PatientVisitContextCodes.EncounterConsultation;
        }
        else if (lastVisit is not null && lastVisit.Status == MedicalVisitStatus.Completed)
        {
            // No active encounter — surface last completed visit's follow-up hint only.
            previousHadFollowUp = !string.IsNullOrWhiteSpace(lastVisit.FollowUpNotes);
        }

        return new PatientVisitContextDto(
            total,
            completed.Count,
            active.Count,
            isReturning,
            relationship,
            treatmentState,
            encounterKind,
            activeVisit is null ? null : ToListItem(activeVisit),
            lastVisit is null ? null : ToListItem(lastVisit),
            previousHadFollowUp);
    }

    private static MedicalVisitListItemDto ToListItem(MedicalVisit v) =>
        new(
            v.Id,
            v.VisitNumber,
            v.VisitDate,
            v.PatientId,
            v.DoctorId,
            v.ClinicId,
            v.AppointmentId,
            v.Status.ToString(),
            v.ChiefComplaint,
            v.DiagnosisNotes,
            v.FollowUpNotes);
}
