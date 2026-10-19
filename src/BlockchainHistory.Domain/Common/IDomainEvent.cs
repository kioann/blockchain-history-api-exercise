using Mediator;

namespace BlockchainHistory.Domain.Common;

/// <summary>Something that happened. Published after saving.</summary>
public interface IDomainEvent : INotification;
