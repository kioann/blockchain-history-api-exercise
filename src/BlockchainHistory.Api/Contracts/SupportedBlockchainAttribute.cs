using System.ComponentModel.DataAnnotations;
using BlockchainHistory.Domain.Chains;

namespace BlockchainHistory.Api.Contracts;

/// <summary>
/// Validates a route value against the supported network slugs (case-insensitive). With [ApiController],
/// a failure automatically becomes a 400 response before the action runs.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
internal sealed class SupportedBlockchainAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        BlockchainNetworks.TryParse(value as string, out _)
            ? ValidationResult.Success
            : new ValidationResult($"Unknown blockchain '{value}'. Supported: {BlockchainNetworks.SupportedSlugs}.");
}
