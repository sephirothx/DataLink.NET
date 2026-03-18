using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DataLink.NET.Framing;

namespace DataLink.NET.Benchmarks;

[MemoryDiagnoser]
public class PacketFormatterBenchmarks
{
    private byte[] _stream = Array.Empty<byte>();
    private static readonly int[] PayloadSizes = { 16, 32, 64, 128 };

    [Params(16, 64, 256)]
    public int ChunkSize { get; set; }

    [Params(0, 10)]
    public int NoiseBurstEvery { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _stream = BuildStream(packetCount: 200, seed: 42, noiseBurstEvery: NoiseBurstEvery);
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

    private static byte[] BuildStream(int packetCount, int seed, int noiseBurstEvery)
    {
        var formatter = new PacketFormatter();
        var random = new Random(seed);

        var stream = new List<byte>();
        stream.Add(0x00);
        stream.Add(0xFF);
        stream.Add(0x01);

        for (int i = 0; i < packetCount; i++)
        {
            if (noiseBurstEvery > 0 && i > 0 && i % noiseBurstEvery == 0)
            {
                // Add both random bytes and malformed framing to stress parser recovery.
                stream.Add((byte)random.Next(0, 256));
                stream.Add((byte)random.Next(0, 256));
                stream.Add(0x10);
                stream.Add(0x02);
                stream.Add(0x77);
                stream.Add(0x10);
                stream.Add(0x04);
            }

            int payloadSize = PayloadSizes[random.Next(PayloadSizes.Length)];
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
