using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FFMpeg.MCP.Host.Models.Output;

namespace FFMpeg.MCP.Host.Services;

public class OperationTrackingService : IProgressReporter
{
    private readonly ILogger<OperationTrackingService> _logger;
    private readonly ConcurrentDictionary<string, OperationProgress> _operations = new();
    private readonly ConcurrentDictionary<string, string> _operationKeyToId = new();

    public event EventHandler<OperationProgressUpdate>? ProgressUpdated;

    public OperationTrackingService(ILogger<OperationTrackingService> logger)
    {
        _logger = logger;
    }

    public string StartOperation(string operationType, int totalSteps, string description = "")
    {
        var operationId = Guid.NewGuid().ToString();
        var progress = new OperationProgress
        {
            OperationId = operationId,
            OperationType = operationType,
            Status = OperationStatus.Pending,
            CurrentStep = 0,
            TotalSteps = totalSteps,
            CurrentOperation = description,
            StartTime = DateTime.UtcNow
        };

        _operations[operationId] = progress;
        _logger.LogInformation("Started operation {OperationId} of type {OperationType}", operationId, operationType);

        return operationId;
    }

    public OperationStartResult StartOperationWithDeduplication(OperationKey operationKey, int totalSteps, string description = "")
    {
        var keyHash = operationKey.GenerateHash();

        if (_operationKeyToId.TryGetValue(keyHash, out var existingOperationId) &&
            _operations.TryGetValue(existingOperationId, out var existingProgress))
        {
            if (existingProgress.Status == OperationStatus.Pending || existingProgress.Status == OperationStatus.Running)
            {
                _logger.LogInformation("Found existing operation {OperationId} for key hash {KeyHash}", existingOperationId, keyHash);
                return new OperationStartResult
                {
                    OperationId = existingOperationId,
                    IsNewOperation = false,
                    Message = "Operation already running with the same parameters"
                };
            }
            else
            {
                _operationKeyToId.TryRemove(keyHash, out _);
            }
        }

        var operationId = Guid.NewGuid().ToString();
        var progress = new OperationProgress
        {
            OperationId = operationId,
            OperationType = operationKey.OperationType,
            Status = OperationStatus.Pending,
            CurrentStep = 0,
            TotalSteps = totalSteps,
            CurrentOperation = description,
            StartTime = DateTime.UtcNow
        };

        _operations[operationId] = progress;
        _operationKeyToId[keyHash] = operationId;

        _logger.LogInformation("Started new operation {OperationId} with key hash {KeyHash}", operationId, keyHash);

        return new OperationStartResult
        {
            OperationId = operationId,
            IsNewOperation = true,
            Message = "New operation started"
        };
    }

    public async Task ReportProgressAsync(OperationProgressUpdate update)
    {
        if (_operations.TryGetValue(update.OperationId, out var progress))
        {
            progress.CurrentStep = update.CurrentStep;
            progress.TotalSteps = update.TotalSteps;
            progress.CurrentOperation = update.CurrentOperation;
            progress.Status = OperationStatus.Running;

            if (update.AdditionalData != null)
            {
                foreach (var kvp in update.AdditionalData)
                {
                    progress.Metadata[kvp.Key] = kvp.Value;
                }
            }

            CalculateEstimatedTimeRemaining(progress);

            _logger.LogDebug("Progress update for {OperationId}: {CurrentStep}/{TotalSteps} - {CurrentOperation}",
                update.OperationId, update.CurrentStep, update.TotalSteps, update.CurrentOperation);

            ProgressUpdated?.Invoke(this, update);
        }

        await Task.CompletedTask;
    }

    public async Task ReportErrorAsync(string operationId, string error)
    {
        if (_operations.TryGetValue(operationId, out var progress))
        {
            progress.Status = OperationStatus.Failed;
            progress.ErrorMessage = error;
            progress.EndTime = DateTime.UtcNow;

            _logger.LogError("Operation {OperationId} failed: {Error}", operationId, error);

            CleanupCompletedOperation(operationId);
        }

        await Task.CompletedTask;
    }

    public async Task ReportCompletionAsync(string operationId, OperationResult result)
    {
        if (_operations.TryGetValue(operationId, out var progress))
        {
            progress.Status = result.Success ? OperationStatus.Completed : OperationStatus.Failed;
            progress.CurrentStep = progress.TotalSteps;
            progress.EndTime = DateTime.UtcNow;
            progress.OutputFiles = result.OutputFiles;

            if (!result.Success)
            {
                progress.ErrorMessage = result.Message;
            }

            _logger.LogInformation("Operation {OperationId} completed with status {Status}",
                operationId, progress.Status);

            CleanupCompletedOperation(operationId);
        }

        await Task.CompletedTask;
    }

    public async Task<OperationProgress?> GetProgressAsync(string operationId)
    {
        _operations.TryGetValue(operationId, out var progress);
        return await Task.FromResult(progress);
    }

    private static void CalculateEstimatedTimeRemaining(OperationProgress progress)
    {
        if (progress.CurrentStep > 0 && progress.TotalSteps > 0)
        {
            var elapsed = DateTime.UtcNow - progress.StartTime;
            var averageTimePerStep = elapsed.TotalSeconds / progress.CurrentStep;
            var remainingSteps = progress.TotalSteps - progress.CurrentStep;
            progress.EstimatedTimeRemaining = TimeSpan.FromSeconds(averageTimePerStep * remainingSteps);
        }
    }

    private void CleanupCompletedOperation(string operationId)
    {
        var keyToRemove = _operationKeyToId.FirstOrDefault(kvp => kvp.Value == operationId).Key;
        if (!string.IsNullOrEmpty(keyToRemove))
        {
            _operationKeyToId.TryRemove(keyToRemove, out _);
            _logger.LogDebug("Cleaned up operation key mapping for {OperationId}", operationId);
        }
    }
}