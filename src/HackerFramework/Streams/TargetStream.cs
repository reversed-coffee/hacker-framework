using System;
using System.ComponentModel;
using System.IO;

using HackerFramework.NativeObjects;

using static HackerFramework.Native;

namespace HackerFramework.Streams;

/// <summary>
///     Represents a stream that can read and write to an external process' memory space.
/// </summary>
public class TargetStream : Stream {
    readonly RtProc _target;

    uint _position;

    public TargetStream(RtProc target, uint position = 0) {
        _target = target;
        _position = position;
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    public override long Length => int.MaxValue; // Virtual memory bounds.

    public override long Position {
        get => _position;
        set => _position = (uint)value;
    }

    public override void Flush() {
        // Stream automatically flushes.
    }

    public override int Read(byte[] buffer, int offset, int count) {
        if (NtReadVirtualMemory(_target.Handle, _position, buffer, count, out var bytesRead) != NtSuccess ||
            bytesRead != count)
            throw new Win32Exception("Failed to read memory.");

        Position += (uint)bytesRead;
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin) {
        return Position = origin switch {
            SeekOrigin.Begin   => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End     => Length + offset,
            _                  => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };
    }

    public override void SetLength(long value) {
        // Length is read-only.
    }

    public override void Write(byte[] buffer, int offset, int count) {
        if (NtWriteVirtualMemory(_target.Handle, _position, buffer, count, out var bytesWritten) != NtSuccess ||
            bytesWritten != count)
            throw new Win32Exception("Failed to write memory.");

        Position += (uint)bytesWritten;
    }
}