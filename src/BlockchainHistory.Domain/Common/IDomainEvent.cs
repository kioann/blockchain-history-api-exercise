using Mediator;

namespace BlockchainHistory.Domain.Common;

/// <summary>Something meaningful that happened in the domain. Published after the change is saved.</summary>
public interface IDomainEvent : INotification;
