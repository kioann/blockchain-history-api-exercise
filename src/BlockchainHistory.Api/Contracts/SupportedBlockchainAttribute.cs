using System.ComponentModel.DataAnnotations;
using BlockchainHistory.Domain.Chains;

namespace BlockchainHistory.Api.Contracts;

/// <summary>Checks the chain in the URL is supported. With [ApiController], a bad value becomes a 400 automatically.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
internal sealed class SupportedBlockchainAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        BlockchainNetworks.TryParse(value as string, out _)
            ? ValidationResult.Success
            : new ValidationResult($"Unknown blockchain '{value}'. Supported: {BlockchainNetworks.SupportedSlugs}.");
}
