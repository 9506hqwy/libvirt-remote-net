# Libvirt remote for .Net

This library is Libvirt remote interface for .NET Standard 2.0.

This library uses [libvirt RPC infrastructure](https://libvirt.org/kbase/internals/rpc.html)
to communicate libvirt server.
The packet encoding and decoding and stub code generation uses [xdr-net](https://github.com/9506hqwy/xdr-net) package.

## Notes

- asynchronous event

  The event callback register method returns callback ID.
  The event stream is acquired from callback ID.
  To stop event stream calls callback deregister method and `DeleteEventStream` method.
  see [PollDomainEvent Example](./Examples/PollDomainEvent/Program.cs).
  The event stream has maximum latest 1000 event.

## Examples

see [Examples](./Examples) directory.

## References

- [Reference Manual for libvirt](https://libvirt.org/html/index.html)
