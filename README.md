# DataLink.NET
A .NET extensible library for sending and receiving formatted packets over a data link.

The library comes with an implementation that allows to send `DLE-STX DLE-ETX` framed packets over `Serial Port`.
`DLE` bytes in the payload are escaped by stuffing them with another `DLE` byte, making a `DLE-DLE` sequence.

### Example of use
```c#
var dataLink = new DataLink();

// Gets the list of supported interfaces
// The default implementation returns the list of COM serial ports
var devices = dataLink.GetDeviceNames();

// Use SelectDevice to select the communication interface
dataLink.SelectDevice(devices[0]);

// Subscribe to the PacketReceived event
// The event will be raised when a new correctly formatted packet is ready
dataLink.PacketReceived += (sender, payload) => Console.WriteLine(Convert.ToHexString(payload));

// Frame the packet and send it over the data link
dataLink.Send(Encoding.ASCII.GetBytes("Hello DataLink!"));
```
### Extending the library
You can customize the communication channel and packet formatting by implementing the interfaces `ICommunicationChannel` and `IPacketFormatter`, and injecting them in the `DataLink` constructor.

### Benchmarks
Run the benchmarks with:
```bash
dotnet run -c Release --project benchmarks/DataLink.NET.Benchmarks/DataLink.NET.Benchmarks.csproj
```

`ChunkSize` simulates the number of bytes arriving per `DataReceived` event. `NoiseBurstEvery` injects malformed frames every N packets to exercise the error-recovery path. Benchmarks use mixed payload sizes (16–128 bytes) to reflect realistic traffic.

**End-to-end receive pipeline** (read + parse + event dispatch):

| Method          | ChunkSize | NoiseBurstEvery | Mean      | Error     | StdDev    | Gen0   | Allocated |
|---------------- |---------- |---------------- |----------:|----------:|----------:|-------:|----------:|
| ReceivePipeline | 16        | 0               | 28.176 us | 0.1537 us | 0.1363 us | 9.0332 |  73.92 KB |
| ReceivePipeline | 16        | 10              | 27.239 us | 0.0967 us | 0.0905 us | 8.6060 |  70.42 KB |
| ReceivePipeline | 64        | 0               | 12.840 us | 0.1677 us | 0.1569 us | 4.3945 |  35.98 KB |
| ReceivePipeline | 64        | 10              | 13.189 us | 0.0591 us | 0.0524 us | 4.2114 |  34.48 KB |
| ReceivePipeline | 256       | 0               |  9.734 us | 0.0297 us | 0.0278 us | 3.2349 |  26.48 KB |
| ReceivePipeline | 256       | 10              | 10.003 us | 0.0156 us | 0.0139 us | 3.1128 |  25.48 KB |

**Isolated packet parser** (parse only, no I/O or event overhead):

| Method | ChunkSize | NoiseBurstEvery | Mean      | Error     | StdDev    | Gen0   | Allocated |
|------- |---------- |---------------- |----------:|----------:|----------:|-------:|----------:|
| Parse  | 16        | 0               | 10.891 us | 0.2046 us | 0.2009 us | 2.0447 |  16.77 KB |
| Parse  | 16        | 10              | 10.683 us | 0.0666 us | 0.0590 us | 1.9531 |  15.95 KB |
| Parse  | 64        | 0               |  6.448 us | 0.0348 us | 0.0308 us | 2.0523 |  16.77 KB |
| Parse  | 64        | 10              |  6.272 us | 0.0292 us | 0.0244 us | 1.9531 |  15.95 KB |
| Parse  | 256       | 0               |  5.467 us | 0.0068 us | 0.0064 us | 2.0523 |  16.77 KB |
| Parse  | 256       | 10              |  5.456 us | 0.0070 us | 0.0062 us | 1.9531 |  15.95 KB |
