using ErpClink.Modules.Queue.Domain.Entries;
using FluentAssertions;

namespace ErpClink.Modules.Queue.UnitTests;

public sealed class QueueEntryAggregateTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void CheckIn_creates_waiting_entry_with_event()
    {
        var utc = DateTime.UtcNow;
        var entry = QueueEntry.CheckIn(Org, Branch, "Q-20260920-0001", Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 20), QueuePriority.Normal, "u1", utc);

        entry.Status.Should().Be(QueueStatus.Waiting);
        entry.CheckInTimeUtc.Should().Be(utc);
        entry.DomainEvents.Should().ContainSingle(e => e is QueueEntryCheckedInDomainEvent);
    }

    [Fact]
    public void Valid_flow_waiting_called_inservice_completed()
    {
        var entry = CreateWaiting();
        entry.Call("u", DateTime.UtcNow);
        entry.StartService("u", DateTime.UtcNow);
        entry.Complete("u", DateTime.UtcNow);
        entry.Status.Should().Be(QueueStatus.Completed);
        entry.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Invalid_transitions_rejected()
    {
        var waiting = CreateWaiting();
        var actComplete = () => waiting.Complete("u", DateTime.UtcNow);
        actComplete.Should().Throw<InvalidOperationException>();

        waiting.Call("u", DateTime.UtcNow);
        waiting.StartService("u", DateTime.UtcNow);
        waiting.Complete("u", DateTime.UtcNow);
        var actCall = () => waiting.Call("u", DateTime.UtcNow);
        actCall.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Skip_and_cancel_from_waiting()
    {
        var skip = CreateWaiting();
        skip.Skip("u", DateTime.UtcNow);
        skip.Status.Should().Be(QueueStatus.Skipped);

        var cancel = CreateWaiting();
        cancel.Cancel("u", DateTime.UtcNow);
        cancel.Status.Should().Be(QueueStatus.Cancelled);
    }

    private static QueueEntry CreateWaiting() =>
        QueueEntry.CheckIn(Org, Branch, "Q-20260920-0099", Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 20), QueuePriority.Normal, "u1", DateTime.UtcNow);
}
