using FileService.Application.Models;
using FluentValidation;
using System.Text.Json;

namespace FileService.Application.Validation;

public sealed class ReplaceUploadRequestValidator : AbstractValidator<ReplaceUploadRequest>
{
    public ReplaceUploadRequestValidator()
    {
        RuleFor(request => request.FileId).NotEmpty();
        RuleFor(request => request.FileName).NotEmpty();
        RuleFor(request => request.MimeType).NotEmpty();
        RuleFor(request => request.Content).NotNull();

        RuleFor(request => request.Metadata)
            .Must(BeJsonObject)
            .When(request => request.Metadata is not null)
            .WithMessage("Metadata must be a JSON object.");
    }

    private static bool BeJsonObject(JsonDocument? metadata)
    {
        return metadata is not null && metadata.RootElement.ValueKind == JsonValueKind.Object;
    }
}
