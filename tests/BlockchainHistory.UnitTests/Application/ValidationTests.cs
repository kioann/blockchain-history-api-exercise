using BlockchainHistory.Application.Common;
using BlockchainHistory.Application.Snapshots.GetHistory;
using BlockchainHistory.Domain.Chains;
using FluentValidation;
using Mediator;

namespace BlockchainHistory.UnitTests.Application;

public sealed class ValidationTests
{
    private readonly GetSnapshotHistoryQueryValidator _validator = new();

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void HistoryValidator_RejectsOutOfRangePaging(int page, int pageSize)
    {
        var result = _validator.Validate(new GetSnapshotHistoryQuery(BlockchainNetwork.BtcMain, page, pageSize));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 100)]
    public void HistoryValidator_AcceptsValidPaging(int page, int pageSize)
    {
        var result = _validator.Validate(new GetSnapshotHistoryQuery(BlockchainNetwork.BtcMain, page, pageSize));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidationBehavior_InvalidMessage_ThrowsWithoutCallingHandler()
    {
        var behavior = new ValidationBehavior<GetSnapshotHistoryQuery, PagedResult<object>>([_validator]);
        var handlerCalled = false;

        await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(
            new GetSnapshotHistoryQuery(BlockchainNetwork.BtcMain, 0, 20),
            (_, _) => { handlerCalled = true; return ValueTask.FromResult(new PagedResult<object>([], 0, 20, 0)); },
            TestContext.Current.CancellationToken).AsTask());

        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task ValidationBehavior_ValidMessage_CallsHandler()
    {
        var behavior = new ValidationBehavior<GetSnapshotHistoryQuery, PagedResult<object>>([_validator]);
        var expected = new PagedResult<object>([], 1, 20, 0);

        var result = await behavior.Handle(
            new GetSnapshotHistoryQuery(BlockchainNetwork.BtcMain, 1, 20),
            (_, _) => ValueTask.FromResult(expected),
            TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
    }
}
