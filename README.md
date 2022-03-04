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

### TODO
- [ ] Writing summaries
