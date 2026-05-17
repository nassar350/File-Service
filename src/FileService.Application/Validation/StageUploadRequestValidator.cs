using FileService.Application.Models;
using FluentValidation;
using System.Text.Json;

namespace FileService.Application.Validation;

public sealed class StageUploadRequestValidator : AbstractValidator<StageUploadRequest>
{
    public StageUploadRequestValidator()
    {
        RuleFor(request => request.TenantId).NotEmpty();
        RuleFor(request => request.OwnerId).NotEmpty();
        RuleFor(request => request.EntityId).NotEmpty();
        RuleFor(request => request.Purpose).NotEmpty();
        RuleFor(request => request.EntityType).NotEmpty();
        RuleFor(request => request.FileName).NotEmpty();
        RuleFor(request => request.MimeType).NotEmpty();
        RuleFor(request => request.Content).NotNull();

        RuleFor(request => request.Metadata)
            .Must(BeJsonObject)
            .When(request => request.Metadata is not null)
            .WithMessage("Metadata must be a JSON object.");

        RuleFor(request => request.StagingTtlMinutes)
            .GreaterThan(0)
            .When(request => request.StagingTtlMinutes.HasValue);
    }

    private static bool BeJsonObject(JsonDocument? metadata)
    {
        return metadata is not null && metadata.RootElement.ValueKind == JsonValueKind.Object;
    }
}
