﻿using System;

namespace com.onlineobject.objectnet.embedded
{
    /// <summary>Contains event data for when a client connects to the server.</summary>
    public class ServerConnectedEventArgs : EventArgs
    {
        /// <summary>The newly connected client.</summary>
        public readonly EmbeddedConnection Client;

        /// <summary>Initializes event data.</summary>
        /// <param name="client">The newly connected client.</param>
        public ServerConnectedEventArgs(EmbeddedConnection client)
        {
            Client = client;
        }
    }

    /// <summary>Contains event data for when a connection fails to be fully established.</summary>
    public class ServerConnectionFailedEventArgs : EventArgs
    {
        /// <summary>The connection that failed to be established.</summary>
        public readonly EmbeddedConnection Client;

        /// <summary>Initializes event data.</summary>
        /// <param name="client">The connection that failed to be established.</param>
        public ServerConnectionFailedEventArgs(EmbeddedConnection client)
        {
            Client = client;
        }
    }

    /// <summary>Contains event data for when a client disconnects from the server.</summary>
    public class ServerDisconnectedEventArgs : EventArgs
    {
        /// <summary>The client that disconnected.</summary>
        public readonly EmbeddedConnection Client;
        /// <summary>The reason for the disconnection.</summary>
        public readonly DisconnectReason Reason;

        /// <summary>Initializes event data.</summary>
        /// <param name="client">The client that disconnected.</param>
        /// <param name="reason">The reason for the disconnection.</param>
        public ServerDisconnectedEventArgs(EmbeddedConnection client, DisconnectReason reason)
        {
            Client = client;
            Reason = reason;
        }
    }

    /// <summary>Contains event data for when a message is received.</summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>The connection from which the message was received.</summary>
        public readonly EmbeddedConnection FromConnection;
        /// <summary>The ID of the message.</summary>
        public readonly ushort MessageId;
        /// <summary>The received message.</summary>
        public readonly EmbeddedMessage Message;

        /// <summary>Initializes event data.</summary>
        /// <param name="fromConnection">The connection from which the message was received.</param>
        /// <param name="messageId">The ID of the message.</param>
        /// <param name="message">The received message.</param>
        public MessageReceivedEventArgs(EmbeddedConnection fromConnection, ushort messageId, EmbeddedMessage message)
        {
            FromConnection = fromConnection;
            MessageId = messageId;
            Message = message;
        }
    }

    /// <summary>Contains event data for when a connection attempt to a server fails.</summary>
    public class ConnectionFailedEventArgs : EventArgs
    {
        /// <summary>The reason for the connection failure.</summary>
        public readonly RejectReason Reason;
        /// <summary>Additional data related to the failed connection attempt (if any).</summary>
        public readonly EmbeddedMessage Message;

        /// <summary>Initializes event data.</summary>
        /// <param name="reason">The reason for the connection failure.</param>
        /// <param name="message">Additional data related to the failed connection attempt (if any).</param>
        public ConnectionFailedEventArgs(RejectReason reason, EmbeddedMessage message)
        {
            Reason = reason;
            Message = message;
        }
    }

    /// <summary>Contains event data for when the client disconnects from a server.</summary>
    public class DisconnectedEventArgs : EventArgs
    {
        /// <summary>The reason for the disconnection.</summary>
        public readonly DisconnectReason Reason;
        /// <summary>Additional data related to the disconnection (if any).</summary>
        public readonly EmbeddedMessage Message;

        /// <summary>Initializes event data.</summary>
        /// <param name="reason">The reason for the disconnection.</param>
        /// <param name="message">Additional data related to the disconnection (if any).</param>
        public DisconnectedEventArgs(DisconnectReason reason, EmbeddedMessage message)
        {
            Reason = reason;
            Message = message;
        }
    }

    /// <summary>Contains event data for when a non-local client connects to the server.</summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        /// <summary>The numeric ID of the client that connected.</summary>
        public readonly ushort Id;

        /// <summary>Initializes event data.</summary>
        /// <param name="id">The numeric ID of the client that connected.</param>
        public ClientConnectedEventArgs(ushort id) => Id = id;
    }

    /// <summary>Contains event data for when a non-local client disconnects from the server.</summary>
    public class ClientDisconnectedEventArgs : EventArgs
    {
        /// <summary>The numeric ID of the client that disconnected.</summary>
        public readonly ushort Id;

        /// <summary>Initializes event data.</summary>
        /// <param name="id">The numeric ID of the client that disconnected.</param>
        public ClientDisconnectedEventArgs(ushort id) => Id = id;
    }
}
