using FileService.Application.Models;
using FluentValidation;

namespace FileService.Application.Validation;

public sealed class DeleteFileRequestValidator : AbstractValidator<DeleteFileRequest>
{
    public DeleteFileRequestValidator()
    {
        RuleFor(request => request.FileId).NotEmpty();
    }
}
