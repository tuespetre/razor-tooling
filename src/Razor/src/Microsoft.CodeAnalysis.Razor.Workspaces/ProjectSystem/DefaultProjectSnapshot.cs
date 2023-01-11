﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal class DefaultProjectSnapshot : ProjectSnapshot
{
    private readonly object _lock;

    private readonly Dictionary<string, DefaultDocumentSnapshot> _documents;

    public DefaultProjectSnapshot(ProjectState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        State = state;

        _lock = new object();
        _documents = new Dictionary<string, DefaultDocumentSnapshot>(FilePathComparer.Instance);
    }

    public ProjectState State { get; }

    public override RazorConfiguration? Configuration => HostProject.Configuration;

    public override IEnumerable<string> DocumentFilePaths => State.Documents.Keys;

    public override string FilePath => State.HostProject.FilePath;

    public override string? RootNamespace => State.HostProject.RootNamespace;

    public override LanguageVersion CSharpLanguageVersion => State.CSharpLanguageVersion;

    public HostProject HostProject => State.HostProject;

    public override VersionStamp Version => State.Version;

    public override IReadOnlyList<TagHelperDescriptor> TagHelpers => State.TagHelpers;

    public override ProjectWorkspaceState? ProjectWorkspaceState => State.ProjectWorkspaceState;

    public override DocumentSnapshot? GetDocument(string filePath)
    {
        lock (_lock)
        {
            if (!_documents.TryGetValue(filePath, out var result) &&
                State.Documents.TryGetValue(filePath, out var state))
            {
                result = new DefaultDocumentSnapshot(this, state);
                _documents.Add(filePath, result);
            }

            return result;
        }
    }

    public override bool IsImportDocument(DocumentSnapshot document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return State.ImportsToRelatedDocuments.ContainsKey(document.TargetPath);
    }

    public override ImmutableArray<DocumentSnapshot> GetRelatedDocuments(DocumentSnapshot document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (!State.ImportsToRelatedDocuments.TryGetValue(document.TargetPath, out var relatedDocuments))
        {
            return ImmutableArray<DocumentSnapshot>.Empty;
        }

        lock (_lock)
        {
            using var _ = ArrayBuilderPool<DocumentSnapshot>.GetPooledObject(out var builder);

            foreach (var relatedDocumentFilePath in relatedDocuments)
            {
                if (GetDocument(relatedDocumentFilePath) is { } relatedDocument)
                {
                    builder.Add(relatedDocument);
                }
            }

            return builder.ToImmutableArray();
        }
    }

    public override RazorProjectEngine GetProjectEngine()
    {
        return State.ProjectEngine;
    }
}
