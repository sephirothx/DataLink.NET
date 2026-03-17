using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace DataLink.NET.Benchmarks;

[MemoryDiagnoser]
public class PacketFormatterBenchmarks
{
    private readonly byte[] _stream;

    [Params(16, 64, 256)]
    public int ChunkSize { get; set; }

    public PacketFormatterBenchmarks()
    {
        _stream = BuildStream(packetCount: 200, payloadSize: 64, seed: 42);
    }

    [Benchmark]
    public int Parse()
    {
        var formatter = new PacketFormatter();
        int packets = 0;

        for (int offset = 0; offset < _stream.Length; offset += ChunkSize)
        {
            int length = Math.Min(ChunkSize, _stream.Length - offset);
            var span = new ReadOnlySpan<byte>(_stream, offset, length);
            formatter.ProcessBytes(span, _ => packets++);
        }

        return packets;
    }

    private static byte[] BuildStream(int packetCount, int payloadSize, int seed)
    {
        var formatter = new PacketFormatter();
        var random = new Random(seed);

        var stream = new List<byte>();
        stream.Add(0x00);
        stream.Add(0xFF);
        stream.Add(0x01);

        for (int i = 0; i < packetCount; i++)
        {
            byte[] payload = new byte[payloadSize];
            random.NextBytes(payload);

            // Force stuffed bytes in every payload to model realistic escaping cost.
            payload[i % payloadSize] = 0x10;

            byte[] packet = formatter.EncodePacket(payload);
            stream.AddRange(packet);
        }

        return stream.ToArray();
    }
}
