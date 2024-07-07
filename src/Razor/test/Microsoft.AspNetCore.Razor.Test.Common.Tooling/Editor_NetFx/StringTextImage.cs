﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.AspNetCore.Razor.Test.Common.Editor;

public class StringTextImage(string text) : ITextImage
{
    private readonly SourceText _sourceText = SourceText.From(text);
    private readonly string _text = text;

    public char this[int position] => _text[position];

    public ITextImageVersion Version => null!;

    public int Length => _text.Length;

    public int LineCount => _sourceText.Lines.Count;

    public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
    {
        _text.CopyTo(sourceIndex, destination, destinationIndex, count);
    }

    public TextImageLine GetLineFromLineNumber(int lineNumber)
    {
        var line = _sourceText.Lines[lineNumber];
        return new TextImageLine(this, lineNumber, new Span(line.Start, line.End - line.Start), line.EndIncludingLineBreak - line.End);
    }

    public TextImageLine GetLineFromPosition(int position)
    {
        var line = _sourceText.Lines.GetLineFromPosition(position);
        return new TextImageLine(this, line.LineNumber, new Span(line.Start, line.End - line.Start), line.EndIncludingLineBreak - line.End);
    }

    public int GetLineNumberFromPosition(int position)
    {
        return _sourceText.Lines.GetLineFromPosition(position).LineNumber;
    }

    public ITextImage GetSubText(Span span)
    {
        return new StringTextImage(_text.Substring(span.Start, span.Length));
    }

    public string GetText(Span span)
    {
        return _text.Substring(span.Start, span.Length);
    }

    public char[] ToCharArray(int startIndex, int length)
    {
        return _text.ToCharArray(startIndex, length);
    }

    public void Write(TextWriter writer, Span span)
    {
        writer.Write(_text.Substring(span.Start, span.Length));
    }
}
