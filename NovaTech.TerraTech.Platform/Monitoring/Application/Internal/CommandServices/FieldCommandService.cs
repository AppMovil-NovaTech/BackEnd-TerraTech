using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Monitoring.Application.Errors;
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Application.Model;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Internal.CommandServices;
public class FieldCommandService(IFieldRepository fieldRepository, IUnitOfWork unitOfWork, ILogger<FieldCommandService> logger) : IFieldCommandService
{
    public async Task<Result<Field>> Handle(CreateFieldCommand command, CancellationToken cancellationToken = default)
    {
        var existingSource = await fieldRepository.FindBySoilTypeAndLocationLatLongAsync(command.SoilType, command.LocationLatLong, cancellationToken);
        if (existingSource != null)
        {
            logger.LogWarning("Duplicate field rejected for SoilType {SoilType} and LocationLatLong {LocationLatLong}", command.SoilType, command.LocationLatLong);
            return Result<Field>.Failure(CreateFieldError.DuplicateField, "A field with the same soil type and location already exists.");
        }

        try
        {
            var field = new Field(command);
            await fieldRepository.AddAsync(field, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Field>.Success(field);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid arguments while creating field for SoilType {SoilType} and LocationLatLong {LocationLatLong}", command.SoilType, command.LocationLatLong);
            return Result<Field>.Failure(CreateFieldError.InvalidData, ex.Message);
        }
        catch (DbUpdateException ex)when (IsDuplicateKeyViolation(ex))
        {
            logger.LogWarning(ex, "Duplicate key violation creating field for SoilType {SoilType} and LocationLatLong {LocationLatLong}", command.SoilType, command.LocationLatLong);
            return Result<Field>.Failure(CreateFieldError.DuplicateField, "Database duplicate key violation occurred.");
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database update failed creating field for SoilType {SoilType} and LocationLatLong {LocationLatLong}", command.SoilType, command.LocationLatLong);
            return Result<Field>.Failure(CreateFieldError.UnexpectedError, "Database update failed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating field for SoilType {SoilType} and LocationLatLong {LocationLatLong}", command.SoilType, command.LocationLatLong);
            return Result<Field>.Failure(CreateFieldError.UnexpectedError, ex.Message);
        }
    }

    public async Task<Result<Field>> Handle(UpdateFieldCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var field = await fieldRepository.FindByIdAsync(command.Id, cancellationToken);
            if (field is null)
            {
                logger.LogWarning("Field with id {Id} not found for update", command.Id);
                return Result<Field>.Failure(CreateFieldError.FieldNotFound, $"Field with id {command.Id} not found.");
            }

            field.Update(command);
            fieldRepository.Update(field);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Field {Id} updated successfully", command.Id);
            return Result<Field>.Success(field);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid arguments while updating field {Id}", command.Id);
            return Result<Field>.Failure(CreateFieldError.InvalidData, ex.Message);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error while updating field {Id}", command.Id);
            return Result<Field>.Failure(CreateFieldError.UnexpectedError, "Database error occurred.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating field {Id}", command.Id);
            return Result<Field>.Failure(CreateFieldError.UnexpectedError, ex.Message);
        }
    }

    public async Task<Result<Field>> Handle(DeleteFieldCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var field = await fieldRepository.FindByIdAsync(command.Id, cancellationToken);
            if (field is null)
            {
                logger.LogWarning("Field with id {Id} not found for deletion", command.Id);
                return Result<Field>.Failure(CreateFieldError.FieldNotFound, $"Field with id {command.Id} not found.");
            }

            fieldRepository.Remove(field);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Field {Id} deleted successfully", command.Id);
            return Result<Field>.Success(field);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error deleting field {Id}", command.Id);
            return Result<Field>.Failure(CreateFieldError.UnexpectedError, ex.Message);
        }
    }

    private static bool IsDuplicateKeyViolation(DbUpdateException exception)
    {
        return DatabaseErrors.IsUniqueViolation(exception);
    }
}
