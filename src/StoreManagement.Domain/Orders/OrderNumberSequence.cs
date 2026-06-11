using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace StoreManagement.Orders;

public class OrderNumberSequence : AuditedAggregateRoot<Guid>
{
    public string Prefix { get; private set; } = string.Empty;

    public int Year { get; private set; }

    public long NextNumber { get; private set; }

    protected OrderNumberSequence()
    {
    }

    public OrderNumberSequence(
        Guid id,
        string prefix,
        int year)
        : base(id)
    {
        SetPrefix(prefix);
        SetYear(year);

        NextNumber = 1;
    }

    public long GetNextNumber()
    {
        var currentNumber = NextNumber;

        NextNumber++;

        return currentNumber;
    }

    private void SetPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderNumberRequired)
                .WithData("PropertyName", nameof(Prefix));
        }

        var normalizedPrefix = prefix.Trim().ToUpperInvariant();

        if (normalizedPrefix.Length > OrderConsts.MaxOrderNumberPrefixLength)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderTextTooLong)
                .WithData("PropertyName", nameof(Prefix))
                .WithData("MaxLength", OrderConsts.MaxOrderNumberPrefixLength);
        }

        Prefix = normalizedPrefix;
    }

    private void SetYear(int year)
    {
        if (year <= 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderNumberRequired)
                .WithData("PropertyName", nameof(Year));
        }

        Year = year;
    }
}