﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

internal class MonitorProjectConfigurationFilePathEndpoint : IMonitorProjectConfigurationFilePathHandler, IDisposable
{
    private readonly ProjectSnapshotManagerDispatcher _projectSnapshotManagerDispatcher;
    private readonly WorkspaceDirectoryPathResolver _workspaceDirectoryPathResolver;
    private readonly IEnumerable<IProjectConfigurationFileChangeListener> _listeners;
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, (string ConfigurationDirectory, IFileChangeDetector Detector)> _outputPathMonitors;
    private readonly object _disposeLock;
    private bool _disposed;

    public bool MutatesSolutionState => false;

    public MonitorProjectConfigurationFilePathEndpoint(
        ProjectSnapshotManagerDispatcher projectSnapshotManagerDispatcher,
        WorkspaceDirectoryPathResolver workspaceDirectoryPathResolver,
        IEnumerable<IProjectConfigurationFileChangeListener> listeners,
        LanguageServerFeatureOptions languageServerFeatureOptions,
        ILoggerFactory loggerFactory)
    {
        if (languageServerFeatureOptions is null)
        {
            throw new ArgumentNullException(nameof(languageServerFeatureOptions));
        }

        _projectSnapshotManagerDispatcher = projectSnapshotManagerDispatcher;
        _workspaceDirectoryPathResolver = workspaceDirectoryPathResolver;
        _listeners = listeners;
        _languageServerFeatureOptions = languageServerFeatureOptions;
        _logger = loggerFactory.CreateLogger<MonitorProjectConfigurationFilePathEndpoint>();
        _outputPathMonitors = new ConcurrentDictionary<string, (string, IFileChangeDetector)>(FilePathComparer.Instance);
        _disposeLock = new object();
    }

    public async Task HandleNotificationAsync(MonitorProjectConfigurationFilePathParams request, RazorRequestContext requestContext, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        lock (_disposeLock)
        {
            if (_disposed)
            {
                return;
            }
        }

        if (request.ConfigurationFilePath is null)
        {
            _logger.LogInformation("'null' configuration path provided. Stopping custom configuration monitoring for project '{0}'.", request.ProjectFilePath);
            RemoveMonitor(request.ProjectFilePath);

            return;
        }

        if (!request.ConfigurationFilePath.EndsWith(_languageServerFeatureOptions.ProjectConfigurationFileName, StringComparison.Ordinal))
        {
            _logger.LogError("Invalid configuration file path provided for project '{0}': '{1}'", request.ProjectFilePath, request.ConfigurationFilePath);
            return;
        }

        var configurationDirectory = Path.GetDirectoryName(request.ConfigurationFilePath);
        var normalizedConfigurationDirectory = FilePathNormalizer.NormalizeDirectory(configurationDirectory);
        var workspaceDirectory = _workspaceDirectoryPathResolver.Resolve();
        var normalizedWorkspaceDirectory = FilePathNormalizer.NormalizeDirectory(workspaceDirectory);

        Assumes.NotNull(configurationDirectory);

        var previousMonitorExists = _outputPathMonitors.TryGetValue(request.ProjectFilePath, out var entry);

        if (normalizedConfigurationDirectory.StartsWith(normalizedWorkspaceDirectory, FilePathComparison.Instance))
        {
            if (previousMonitorExists)
            {
                _logger.LogInformation("Configuration directory changed from external directory -> internal directory for project '{0}, terminating existing monitor'.", request.ProjectFilePath);
                RemoveMonitor(request.ProjectFilePath);
            }
            else
            {
                _logger.LogInformation("No custom configuration directory required. The workspace directory is sufficient for '{0}'.", request.ProjectFilePath);
            }

            // Configuration directory is already in the workspace directory. We already monitor everything in the workspace directory.
            return;
        }

        if (previousMonitorExists)
        {
            if (FilePathComparer.Instance.Equals(configurationDirectory, entry.ConfigurationDirectory))
            {
                _logger.LogInformation("Already tracking configuration directory for project '{0}'.", request.ProjectFilePath);

                // Already tracking the requested configuration output path for this project
                return;
            }

            _logger.LogInformation("Project configuration output path has changed. Stopping existing monitor for project '{0}' so we can restart it with a new directory.", request.ProjectFilePath);
            RemoveMonitor(request.ProjectFilePath);
        }

        var detector = CreateFileChangeDetector();
        entry = (configurationDirectory, detector);

        if (!_outputPathMonitors.TryAdd(request.ProjectFilePath, entry))
        {
            // There's a concurrent request going on for this specific project. To avoid calling "StartAsync" twice we return early.
            // Note: This is an extremely edge case race condition that should in practice never happen due to how long it takes to calculate project state changes
            return;
        }

        _logger.LogInformation("Starting new configuration monitor for project '{0}' for directory '{1}'.", request.ProjectFilePath, configurationDirectory);
        await entry.Detector.StartAsync(configurationDirectory, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            // Request was cancelled while starting the detector. Need to stop it so we don't leak.
            entry.Detector.Stop();
            return;
        }

        if (!_outputPathMonitors.ContainsKey(request.ProjectFilePath))
        {
            // This can happen if there were multiple concurrent requests to "remove" and "update" file change detectors for the same project path.
            // In that case we need to stop the detector to ensure we don't leak.
            entry.Detector.Stop();
            return;
        }

        lock (_disposeLock)
        {
            if (_disposed)
            {
                // Server's being stopped.
                entry.Detector.Stop();
            }
        }
    }

    private void RemoveMonitor(string projectFilePath)
    {
        // Should no longer monitor configuration output paths for the project
        if (_outputPathMonitors.TryRemove(projectFilePath, out var removedEntry))
        {
            removedEntry.Detector.Stop();
        }
        else
        {
            // Concurrent requests to remove the same configuration output path for the project.  We've already
            // done the removal so we can just return gracefully.
        }
    }

    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        foreach (var entry in _outputPathMonitors)
        {
            entry.Value.Detector.Stop();
        }
    }

    // Protected virtual for testing
    protected virtual IFileChangeDetector CreateFileChangeDetector() => new ProjectConfigurationFileChangeDetector(
        _projectSnapshotManagerDispatcher,
        _listeners,
        _languageServerFeatureOptions);
}
