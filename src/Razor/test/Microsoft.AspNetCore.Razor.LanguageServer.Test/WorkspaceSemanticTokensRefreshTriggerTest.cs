﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

public class WorkspaceSemanticTokensRefreshTriggerTest : LanguageServerTestBase
{
    private readonly TestProjectSnapshotManager _projectManager;
    private readonly HostProject _hostProject;
    private readonly HostDocument _hostDocument;

    public WorkspaceSemanticTokensRefreshTriggerTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _projectManager = TestProjectSnapshotManager.Create(Dispatcher, ErrorReporter);
        _projectManager.AllowNotifyListeners = true;
        _hostProject = new HostProject("/path/to/project.csproj", "/path/to/obj", RazorConfiguration.Default, "TestRootNamespace");
        _hostDocument = new HostDocument("/path/to/file.razor", "file.razor");
    }

    protected override async Task InitializeAsync()
    {
        await RunOnDispatcherAsync(() =>
        {
            _projectManager.ProjectAdded(_hostProject);
            _projectManager.DocumentAdded(_hostProject.Key, _hostDocument, new EmptyTextLoader(_hostDocument.FilePath));
        });
    }

    [Fact]
    public async Task PublishesOnWorkspaceUpdate()
    {
        // Arrange
        var workspaceChangedPublisher = new Mock<IWorkspaceSemanticTokensRefreshPublisher>(MockBehavior.Strict);
        workspaceChangedPublisher.Setup(w => w.EnqueueWorkspaceSemanticTokensRefresh());
        var defaultWorkspaceChangedRefresh = new TestDefaultWorkspaceSemanticTokensRefreshTrigger(workspaceChangedPublisher.Object);
        defaultWorkspaceChangedRefresh.Initialize(_projectManager);

        // Act
        var newDocument = new HostDocument("/path/to/newFile.razor", "newFile.razor");

        await RunOnDispatcherAsync(() =>
            _projectManager.DocumentAdded(_hostProject.Key, newDocument, new EmptyTextLoader(newDocument.FilePath)));

        // Assert
        workspaceChangedPublisher.VerifyAll();
    }

    private class TestDefaultWorkspaceSemanticTokensRefreshTrigger : WorkspaceSemanticTokensRefreshTrigger
    {
        internal TestDefaultWorkspaceSemanticTokensRefreshTrigger(IWorkspaceSemanticTokensRefreshPublisher workspaceSemanticTokensRefreshPublisher)
            : base(workspaceSemanticTokensRefreshPublisher)
        {
        }
    }

    private class TestErrorReporter : IErrorReporter
    {
        public void ReportError(Exception exception) => throw new NotImplementedException();
        public void ReportError(Exception exception, IProjectSnapshot? project) => throw new NotImplementedException();
        public void ReportError(Exception exception, Project workspaceProject) => throw new NotImplementedException();
    }
}
