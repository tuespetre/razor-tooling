﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.PooledObjects;

namespace System.Buffers;

internal static class BufferExtensions
{
    /// <summary>
    ///  Rents an array of the given minimum length from the specified <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <remarks>
    ///  The array is guaranteed to be at least <paramref name="minimumLength"/> in length. However,
    ///  it will likely be larger.
    /// </remarks>
    public static PooledArray<T> GetPooledArray<T>(this ArrayPool<T> pool, int minimumLength, out T[] array)
    {
        var result = new PooledArray<T>(pool, minimumLength);
        array = result.Array;
        return result;
    }

    /// <summary>
    ///  Rents an array of the given minimum length from the specified <see cref="ArrayPool{T}"/>.
    ///  The rented array is provided as a <see cref="Span{T}"/> representing a portion of the rented array
    ///  from its start to the minimum length.
    /// </summary>
    public static PooledArray<T> GetPooledArraySpan<T>(this ArrayPool<T> pool, int minimumLength, out Span<T> span)
    {
        var result = new PooledArray<T>(pool, minimumLength);
        span = result.Span;
        return result;
    }
}
