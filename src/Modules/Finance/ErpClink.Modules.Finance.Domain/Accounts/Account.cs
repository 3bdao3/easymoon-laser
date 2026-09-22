using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Finance.Domain.Accounts;

public sealed class Account : AggregateRoot
{
    private Account()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? ParentAccountId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AccountType AccountType { get; private set; }
    public bool IsPostable { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static Account Create(
        Guid organizationId,
        string code,
        string name,
        string? description,
        AccountType accountType,
        bool isPostable,
        Guid? parentAccountId,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var account = new Account
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ParentAccountId = parentAccountId,
            Code = Require(code, 32),
            Name = Require(name, 200),
            Description = Normalize(description, 500),
            AccountType = accountType,
            IsPostable = isPostable,
            IsActive = true
        };
        account.SetCreated(createdBy, utcNow);
        account.RaiseDomainEvent(new AccountCreatedDomainEvent(account.Id, utcNow));
        return account;
    }

    public void Update(
        string name,
        string? description,
        AccountType accountType,
        bool isPostable,
        Guid? parentAccountId,
        string? updatedBy,
        DateTime utcNow)
    {
        if (parentAccountId == Id)
            throw new InvalidOperationException("An account cannot be its own parent.");

        Name = Require(name, 200);
        Description = Normalize(description, 500);
        AccountType = accountType;
        IsPostable = isPostable;
        ParentAccountId = parentAccountId;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new AccountUpdatedDomainEvent(Id, utcNow));
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new AccountUpdatedDomainEvent(Id, utcNow));
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new AccountUpdatedDomainEvent(Id, utcNow));
    }

    public void EnsureUsableForJournalLine()
    {
        if (!IsActive)
            throw new InvalidOperationException("Inactive accounts cannot be used on journal lines.");
        if (!IsPostable)
            throw new InvalidOperationException("Non-postable accounts cannot be used on journal lines.");
    }

    private static string Require(string value, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

public sealed class AccountCreatedDomainEvent : IDomainEvent
{
    public AccountCreatedDomainEvent(Guid accountId, DateTime occurredOnUtc)
    {
        AccountId = accountId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid AccountId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class AccountUpdatedDomainEvent : IDomainEvent
{
    public AccountUpdatedDomainEvent(Guid accountId, DateTime occurredOnUtc)
    {
        AccountId = accountId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid AccountId { get; }
    public DateTime OccurredOnUtc { get; }
}
