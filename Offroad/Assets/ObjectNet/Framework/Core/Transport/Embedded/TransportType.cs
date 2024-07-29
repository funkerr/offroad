namespace com.onlineobject.objectnet {

    // Define a custom attribute to specify the transport type characteristics.
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TransportType : System.Attribute {

        // Indicates whether the transport type uses a double channel.
        private bool DoubleChannel = false;

        // Indicates whether the transport support peer to peer.
        private bool PeerToPeerSupport = false;

        /// <summary>
        /// Initializes a new instance of the TransportType attribute with the specified channel type.
        /// </summary>
        /// <param name="BiChannel">If set to true, the transport type is considered to have a double channel.</param>
        /// <param name="PeerToPeerSupport">If set to true, the transport type will support peer to peer.</param>
        public TransportType(bool BiChannel, bool PeerToPeerSupport) {
            this.DoubleChannel = BiChannel;
            this.PeerToPeerSupport = PeerToPeerSupport;
        }

        /// <summary>
        /// Initializes a new instance of the TransportType attribute with the specified channel type.
        /// </summary>
        /// <param name="PeerToPeerSupport">If set to true, the transport type will support peer to peer.</param>
        public TransportType(bool PeerToPeerSupport) {
            this.DoubleChannel = false;
            this.PeerToPeerSupport = PeerToPeerSupport;
        }

        /// <summary>
        /// Gets a value indicating whether the transport type uses a double channel.
        /// </summary>
        /// <returns>Returns true if the transport type is double channel; otherwise, false.</returns>
        public bool IsDoubleChannel() => DoubleChannel;

        /// <summary>
        /// Gets a value indicating whether the transport support peer to peer.
        /// </summary>
        /// <returns>Returns true if the transport type supports peer to peer; otherwise, false.</returns>
        public bool IsPeerToPeerSupported() => PeerToPeerSupport;
    }

}