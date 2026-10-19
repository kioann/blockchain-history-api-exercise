using BlockchainHistory.Domain.Snapshots;

namespace BlockchainHistory.Tests.Shared;

/// <summary>Real BTC and ETH data from BlockCypher (same as the JSON fixtures).</summary>
internal static class TestChainStatus
{
    public static ChainStatus Utxo() => new()
    {
        Name = "BTC.main",
        Height = 971714,
        Hash = "0000000000000000000138485e62e351a291965229fce91e716331049000095b",
        Time = new DateTimeOffset(2026, 10, 16, 13, 4, 54, TimeSpan.Zero).AddTicks(9635951),
        LatestUrl = "https://api.blockcypher.com/v1/btc/main/blocks/0000000000000000000138485e62e351a291965229fce91e716331049000095b",
        PreviousHash = "000000000000000000001124b83cb51bf2f9b8f74172ef0a5f2eef429a666b27",
        PreviousUrl = "https://api.blockcypher.com/v1/btc/main/blocks/000000000000000000001124b83cb51bf2f9b8f74172ef0a5f2eef429a666b27",
        PeerCount = 324,
        UnconfirmedCount = 8250,
        LastForkHeight = 966500,
        LastForkHash = "000000000000000000011ada8812e1b93daabaea5f4f90b1920e8212a11a36ca",
        HighFeePerKb = 2915,
        MediumFeePerKb = 1756,
        LowFeePerKb = 1396,
    };

    public static ChainStatus Ethereum() => new()
    {
        Name = "ETH.main",
        Height = 26205424,
        Hash = "c38793292c8b8bc57ab37ad14854b99ac772c770583c354f5d258584ba8e5e74",
        Time = new DateTimeOffset(2026, 10, 16, 13, 34, 15, TimeSpan.Zero).AddTicks(2488903),
        LatestUrl = "https://api.blockcypher.com/v1/eth/main/blocks/c38793292c8b8bc57ab37ad14854b99ac772c770583c354f5d258584ba8e5e74",
        PreviousHash = "00f4c79287608104c028542af9186d2f64a961dccdb12b846eef0ffe480a66ec",
        PreviousUrl = "https://api.blockcypher.com/v1/eth/main/blocks/00f4c79287608104c028542af9186d2f64a961dccdb12b846eef0ffe480a66ec",
        PeerCount = 0,
        UnconfirmedCount = 4,
        LastForkHeight = 26197519,
        LastForkHash = "5842fbae9a0dbb5b41fd2a6caaf605e638221d534b586a8043f8a443c019bec3",
        HighGasPrice = 3439662035,
        MediumGasPrice = 2492686971,
        LowGasPrice = 1376987995,
        HighPriorityFee = 1752375086,
        MediumPriorityFee = 445410673,
        LowPriorityFee = 210781767,
        BaseFee = 155630516,
    };
}
