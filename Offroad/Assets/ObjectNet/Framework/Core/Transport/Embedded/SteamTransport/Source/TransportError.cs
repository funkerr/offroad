namespace com.onlineobject.objectnet.steamworks {
    public enum TransportError : byte
    {
        DnsResolve,       // failed to resolve a host name
        Refused,          // connection refused by other end. server full etc.
        Timeout,          // ping timeout or dead link
        Congestion,       // more messages than transport / network can process
        InvalidReceive,   // recv invalid packet (possibly intentional attack)
        InvalidSend,      // user tried to send invalid data
        ConnectionClosed, // connection closed voluntarily or lost involuntarily
        Unexpected        // unexpected error / exception, requires fix.
    }
}
