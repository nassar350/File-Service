using FileService.Application.Models;
using FluentValidation;

namespace FileService.Application.Validation;

public sealed class CommitUploadRequestValidator : AbstractValidator<CommitUploadRequest>
{
    public CommitUploadRequestValidator()
    {
        RuleFor(request => request.FileId).NotEmpty();

        RuleFor(request => request.ReplacesFileId)
            .NotEqual(request => request.FileId)
            .When(request => request.ReplacesFileId.HasValue);
    }
}
