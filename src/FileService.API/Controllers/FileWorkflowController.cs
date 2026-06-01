using System.Text.Json;
using FileService.Application.Abstractions.Services;
using FileService.Application.Models;
using FileService.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FileService.API.Controllers;

[ApiController]
[Route("v1/uploads/sessions")]
public sealed class FileWorkflowController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> StageUploadAsync(
        [FromForm] StageUploadForm form,
        [FromServices] IFileWorkflowService service,
        [FromServices] IValidator<StageUploadRequest> validator,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest("File is required.");
        }

        if (!TryParseMetadata(form.Metadata, out var metadata, out var error))
        {
            return BadRequest(error);
        }

        await using var stream = form.File.OpenReadStream();

        var request = new StageUploadRequest(
            form.TenantId,
            form.OwnerId,
            form.Purpose,
            form.EntityType,
            form.EntityId,
            form.File.FileName,
            form.File.ContentType,
            stream,
            metadata,
            form.Provider,
            form.StagingTtlMinutes);

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToValidationProblem(validation));
        }

        var file = await service.StageUploadAsync(request, cancellationToken);
        return Ok(FileWorkflowResponse.From(file));
    }

    [HttpPost("{fileId:guid}/commit")]
    public async Task<IActionResult> CommitUploadAsync(
        Guid fileId,
        [FromBody] CommitUploadBody? body,
        [FromServices] IFileWorkflowService service,
        [FromServices] IValidator<CommitUploadRequest> validator,
        CancellationToken cancellationToken)
    {
        var request = new CommitUploadRequest(
            fileId,
            body?.DestinationFolder,
            body?.ReplacesFileId,
            body?.IsPrivate ?? false);

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToValidationProblem(validation));
        }

        var file = await service.CommitUploadAsync(request, cancellationToken);
        return Ok(FileWorkflowResponse.From(file));
    }

    [HttpPost("{fileId:guid}/replace")]
    public async Task<IActionResult> ReplaceUploadAsync(
        Guid fileId,
        [FromForm] ReplaceUploadForm form,
        [FromServices] IFileWorkflowService service,
        [FromServices] IValidator<ReplaceUploadRequest> validator,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest("File is required.");
        }

        if (!TryParseMetadata(form.Metadata, out var metadata, out var error))
        {
            return BadRequest(error);
        }

        await using var stream = form.File.OpenReadStream();

        var request = new ReplaceUploadRequest(
            fileId,
            form.File.FileName,
            form.File.ContentType,
            stream,
            metadata);

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToValidationProblem(validation));
        }

        var file = await service.ReplaceUploadAsync(request, cancellationToken);
        return Ok(FileWorkflowResponse.From(file));
    }

    [HttpPost("{fileId:guid}/abort")]
    public async Task<IActionResult> AbortUploadAsync(
        Guid fileId,
        [FromServices] IFileWorkflowService service,
        CancellationToken cancellationToken)
    {
        await service.AbortUploadAsync(fileId, cancellationToken);
        return NoContent();
    }

    [HttpGet("/v1/files/{fileId:guid}")]
    public async Task<IActionResult> GetFileAsync(
        Guid fileId,
        [FromServices] IFileWorkflowService service,
        CancellationToken cancellationToken)
    {
        var file = await service.GetByIdAsync(fileId, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        var fileLink = await service.GetDownloadUrlAsync(file, cancellationToken);
        return Ok(FileWorkflowResponse.From(file, fileLink));
    }

    [HttpDelete("/v1/files/{fileId:guid}")]
    public async Task<IActionResult> DeleteFileAsync(
        Guid fileId,
        [FromServices] IFileWorkflowService service,
        [FromServices] IValidator<DeleteFileRequest> validator,
        CancellationToken cancellationToken)
    {
        var request = new DeleteFileRequest(fileId);
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToValidationProblem(validation));
        }

        await service.DeleteFileAsync(request, cancellationToken);
        return NoContent();
    }

    private static bool TryParseMetadata(
        string? raw,
        out JsonDocument? metadata,
        out string? error)
    {
        metadata = null;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        try
        {
            metadata = JsonDocument.Parse(raw);
            return true;
        }
        catch (JsonException)
        {
            error = "Metadata must be valid JSON.";
            return false;
        }
    }

    private static ValidationProblemDetails ToValidationProblem(FluentValidation.Results.ValidationResult validation)
    {
        var errors = validation.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors);
    }

    public sealed record StageUploadForm(
        Guid TenantId,
        Guid OwnerId,
        string Purpose,
        string EntityType,
        Guid EntityId,
        IFormFile File,
        string? Metadata,
        FileProvider? Provider,
        int? StagingTtlMinutes);

    public sealed record ReplaceUploadForm(
        IFormFile File,
        string? Metadata);

    public sealed record CommitUploadBody(
        string? DestinationFolder,
        Guid? ReplacesFileId,
        bool IsPrivate = false);
}
