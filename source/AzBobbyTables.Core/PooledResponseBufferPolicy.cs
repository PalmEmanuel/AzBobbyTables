using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;

namespace PipeHow.AzBobbyTables.Core;

/// <summary>
/// Reads the body of a successful GET into pooled memory, instead of the MemoryStream the pipeline would
/// otherwise grow it into. The body is still read inside the retry loop, so a connection dropped mid-read
/// is retried exactly as before; error responses are buffered as before.
/// </summary>
internal sealed class PooledResponseBufferPolicy : HttpPipelinePolicy
{
    public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        if (message.Request.Method != RequestMethod.Get)
        {
            ProcessNext(message, pipeline);
            return;
        }

        message.BufferResponse = false;
        ProcessNext(message, pipeline);
        if (message.HasResponse && message.Response.ContentStream is { } network)
        {
            message.Response.ContentStream = IsSuccess(message) ? PooledBufferStream.ReadFrom(network) : CopyToMemory(network);
            network.Dispose();
        }
    }

    public override async ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        if (message.Request.Method != RequestMethod.Get)
        {
            await ProcessNextAsync(message, pipeline).ConfigureAwait(false);
            return;
        }

        message.BufferResponse = false;
        await ProcessNextAsync(message, pipeline).ConfigureAwait(false);
        if (message.HasResponse && message.Response.ContentStream is { } network)
        {
            message.Response.ContentStream = IsSuccess(message)
                ? await PooledBufferStream.ReadFromAsync(network, message.CancellationToken).ConfigureAwait(false)
                : await CopyToMemoryAsync(network, message.CancellationToken).ConfigureAwait(false);
            network.Dispose();
        }
    }

    private static bool IsSuccess(HttpMessage message) => message.Response.Status >= 200 && message.Response.Status < 300;

    private static Stream CopyToMemory(Stream source)
    {
        var buffer = new MemoryStream();
        source.CopyTo(buffer);
        buffer.Position = 0;
        return buffer;
    }

    private static async Task<Stream> CopyToMemoryAsync(Stream source, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, 81920, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;
        return buffer;
    }
}

/// <summary>
/// A read-only, seekable stream over a pooled buffer, returned to the pool on dispose.
/// </summary>
/// <remarks>
/// The pool is private because the shared pool releases large arrays on gen2 collections, which reads of large
/// tables trigger constantly. It keeps at most 4 buffers per size up to 4 MB, which is the largest page Azure
/// Table storage returns; larger responses are not pooled.
/// </remarks>
internal sealed class PooledBufferStream : Stream
{
    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Create(4 * 1024 * 1024, 4);
    private byte[]? _buffer = Pool.Rent(81920);
    private int _length;
    private int _position;

    public static PooledBufferStream ReadFrom(Stream source)
    {
        var stream = new PooledBufferStream();
        try
        {
            int read;
            while ((read = source.Read(stream._buffer!, stream._length, stream._buffer!.Length - stream._length)) > 0)
            {
                stream.Advance(read);
            }
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public static async Task<Stream> ReadFromAsync(Stream source, CancellationToken cancellationToken)
    {
        var stream = new PooledBufferStream();
        try
        {
            int read;
            while ((read = await source.ReadAsync(stream._buffer!, stream._length, stream._buffer!.Length - stream._length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                stream.Advance(read);
            }
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private void Advance(int read)
    {
        _length += read;
        if (_length < _buffer!.Length)
        {
            return;
        }

        var larger = Pool.Rent(_buffer.Length * 2);
        Buffer.BlockCopy(_buffer, 0, larger, 0, _length);
        Pool.Return(_buffer);
        _buffer = larger;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var source = _buffer ?? throw new ObjectDisposedException(nameof(PooledBufferStream));
        var available = Math.Min(count, _length - _position);
        if (available <= 0)
        {
            return 0;
        }

        Buffer.BlockCopy(source, _position, buffer, offset, available);
        _position += available;
        return available;
    }

    public override bool CanRead => _buffer is not null;
    public override bool CanSeek => _buffer is not null;
    public override bool CanWrite => false;
    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => _position = (int)Math.Max(0, Math.Min(value, _length));
    }

    public override long Seek(long offset, SeekOrigin origin) => Position = origin switch
    {
        SeekOrigin.Begin => offset,
        SeekOrigin.Current => _position + offset,
        _ => _length + offset,
    };

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        // Exchanged so that two disposals cannot return the same buffer twice and hand it to two readers.
        if (Interlocked.Exchange(ref _buffer, null) is { } buffer)
        {
            Pool.Return(buffer);
        }
        base.Dispose(disposing);
    }
}
