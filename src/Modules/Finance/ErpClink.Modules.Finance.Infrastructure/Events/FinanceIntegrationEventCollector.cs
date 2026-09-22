using ErpClink.BuildingBlocks.Application.IntegrationEvents;

namespace ErpClink.Modules.Finance.Infrastructure.Events;

/// <summary>
/// Test/observability collector for integration events until Outbox exists (STEP 24).
/// </summary>
public sealed class FinanceIntegrationEventCollector
{
    private readonly List<IIntegrationEvent> _events = [];

    public IReadOnlyList<IIntegrationEvent> Events
    {
        get { lock (_events) return _events.ToList(); }
    }

    public void Add(IIntegrationEvent integrationEvent)
    {
        lock (_events) _events.Add(integrationEvent);
    }

    public void Clear()
    {
        lock (_events) _events.Clear();
    }
}
