using System;
using System.Collections.Generic;
using DataLink.NET.Interfaces;
using BenchmarkDotNet.Attributes;

namespace DataLink.NET.Benchmarks;

[MemoryDiagnoser]
public class DataLinkEndToEndBenchmarks
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
    public int ReceivePipeline()
    {
        var channel = new ReplayCommunicationChannel(_stream, ChunkSize);
        using var link = new DataLink(new PacketFormatter(), channel);

        int packets = 0;
        link.PacketReceived += (_, _) => packets++;

        channel.Play();
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
            payload[i % payloadSize] = 0x10;
            byte[] packet = formatter.EncodePacket(payload);
            stream.AddRange(packet);
        }

        return stream.ToArray();
    }

    private sealed class ReplayCommunicationChannel : ICommunicationChannel
    {
        private readonly byte[] _data;
        private readonly int _chunkSize;
        private int _readOffset;
        private int _chunkRemaining;

        public ReplayCommunicationChannel(byte[] data, int chunkSize)
        {
            _data = data;
            _chunkSize = chunkSize;
        }

        public event EventHandler DataReceived;

        public int BytesToRead => _chunkRemaining;

        public string[] GetDeviceNames() => Array.Empty<string>();

        public void SelectDevice(string device)
        {
        }

        public void Send(byte[] buffer)
        {
        }

        public byte ReceiveByte()
        {
            if (_chunkRemaining <= 0)
            {
                throw new InvalidOperationException("No bytes available to read.");
            }

            byte value = _data[_readOffset++];
            _chunkRemaining--;
            return value;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (_chunkRemaining <= 0)
            {
                return 0;
            }

            int bytesRead = Math.Min(count, _chunkRemaining);
            Array.Copy(_data, _readOffset, buffer, offset, bytesRead);
            _readOffset += bytesRead;
            _chunkRemaining -= bytesRead;
            return bytesRead;
        }

        public void Play()
        {
            while (_readOffset < _data.Length)
            {
                _chunkRemaining = Math.Min(_chunkSize, _data.Length - _readOffset);
                DataReceived?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
