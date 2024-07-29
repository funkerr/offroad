﻿using com.onlineobject.objectnet.embedded.Utils;
using System;
using System.Reflection;

namespace com.onlineobject.objectnet.embedded {
    /// <summary>The exception that is thrown when a <see cref="EmbeddedMessage"/> does not contain enough unwritten bits to perform an operation.</summary>
    public class InsufficientCapacityException : Exception
    {
        /// <summary>The message with insufficient remaining capacity.</summary>
        public readonly EmbeddedMessage ExceptionMessage;
        /// <summary>The name of the type which could not be added to the message.</summary>
        public readonly string TypeName;
        /// <summary>The number of available bits the type requires in order to be added successfully.</summary>
        public readonly int RequiredBits;

        /// <summary>Initializes a new <see cref="InsufficientCapacityException"/> instance.</summary>
        public InsufficientCapacityException() { }
        /// <summary>Initializes a new <see cref="InsufficientCapacityException"/> instance with a specified error message.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public InsufficientCapacityException(string message) : base(message) { }
        /// <summary>Initializes a new <see cref="InsufficientCapacityException"/> instance with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception. If <paramref name="inner"/> is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public InsufficientCapacityException(string message, Exception inner) : base(message, inner) { }
        /// <summary>Initializes a new <see cref="InsufficientCapacityException"/> instance and constructs an error message from the given information.</summary>
        /// <param name="message">The message with insufficient remaining capacity.</param>
        /// <param name="reserveBits">The number of bits which were attempted to be reserved.</param>
        public InsufficientCapacityException(EmbeddedMessage message, int reserveBits) : base(GetErrorMessage(message, reserveBits))
        {
            ExceptionMessage = message;
            TypeName = "reservation";
            RequiredBits = reserveBits;
        }
        /// <summary>Initializes a new <see cref="InsufficientCapacityException"/> instance and constructs an error message from the given information.</summary>
        /// <param name="message">The message with insufficient remaining capacity.</param>
        /// <param name="typeName">The name of the type which could not be added to the message.</param>
        /// <param name="requiredBits">The number of available bits required for the type to be added successfully.</param>
        public InsufficientCapacityException(EmbeddedMessage message, string typeName, int requiredBits) : base(GetErrorMessage(message, typeName, requiredBits))
        {
            ExceptionMessage = message;
            TypeName = typeName;
            RequiredBits = requiredBits;
        }
        /// <summary>Initializes a new <see cref="InsufficientCapacityException"/> instance and constructs an error message from the given information.</summary>
        /// <param name="message">The message with insufficient remaining capacity.</param>
        /// <param name="arrayLength">The length of the array which could not be added to the message.</param>
        /// <param name="typeName">The name of the array's type.</param>
        /// <param name="requiredBits">The number of available bits required for a single element of the array to be added successfully.</param>
        public InsufficientCapacityException(EmbeddedMessage message, int arrayLength, string typeName, int requiredBits) : base(GetErrorMessage(message, arrayLength, typeName, requiredBits))
        {
            ExceptionMessage = message;
            TypeName = $"{typeName}[]";
            RequiredBits = requiredBits * arrayLength;
        }

        /// <summary>Constructs the error message from the given information.</summary>
        /// <returns>The error message.</returns>
        private static string GetErrorMessage(EmbeddedMessage message, int reserveBits)
        {
            return $"Cannot reserve {reserveBits} {EmbeddedHelper.CorrectForm(reserveBits, "bit")} in a message with {message.UnwrittenBits} " +
                   $"{EmbeddedHelper.CorrectForm(message.UnwrittenBits, "bit")} of remaining capacity!";
        }
        /// <summary>Constructs the error message from the given information.</summary>
        /// <returns>The error message.</returns>
        private static string GetErrorMessage(EmbeddedMessage message, string typeName, int requiredBits)
        {
            return $"Cannot add a value of type '{typeName}' (requires {requiredBits} {EmbeddedHelper.CorrectForm(requiredBits, "bit")}) to " +
                   $"a message with {message.UnwrittenBits} {EmbeddedHelper.CorrectForm(message.UnwrittenBits, "bit")} of remaining capacity!";
        }
        /// <summary>Constructs the error message from the given information.</summary>
        /// <returns>The error message.</returns>
        private static string GetErrorMessage(EmbeddedMessage message, int arrayLength, string typeName, int requiredBits)
        {
            requiredBits *= arrayLength;
            return $"Cannot add an array of type '{typeName}[]' with {arrayLength} {EmbeddedHelper.CorrectForm(arrayLength, "element")} (requires {requiredBits} {EmbeddedHelper.CorrectForm(requiredBits, "bit")}) " +
                   $"to a message with {message.UnwrittenBits} {EmbeddedHelper.CorrectForm(message.UnwrittenBits, "bit")} of remaining capacity!";
        }
    }
    
    /// <summary>The exception that is thrown when a method with a <see cref="EmbeddedMessageHandlerAttribute"/> is not marked as <see langword="static"/>.</summary>
    public class NonStaticHandlerException : Exception
    {
        /// <summary>The type containing the handler method.</summary>
        public readonly Type DeclaringType;
        /// <summary>The name of the handler method.</summary>
        public readonly string HandlerMethodName;

        /// <summary>Initializes a new <see cref="NonStaticHandlerException"/> instance.</summary>
        public NonStaticHandlerException() { }
        /// <summary>Initializes a new <see cref="NonStaticHandlerException"/> instance with a specified error message.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public NonStaticHandlerException(string message) : base(message) { }
        /// <summary>Initializes a new <see cref="NonStaticHandlerException"/> instance with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception. If <paramref name="inner"/> is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public NonStaticHandlerException(string message, Exception inner) : base(message, inner) { }
        /// <summary>Initializes a new <see cref="NonStaticHandlerException"/> instance and constructs an error message from the given information.</summary>
        /// <param name="declaringType">The type containing the handler method.</param>
        /// <param name="handlerMethodName">The name of the handler method.</param>
        public NonStaticHandlerException(Type declaringType, string handlerMethodName) : base(GetErrorMessage(declaringType, handlerMethodName))
        {
            DeclaringType = declaringType;
            HandlerMethodName = handlerMethodName;
        }

        /// <summary>Constructs the error message from the given information.</summary>
        /// <returns>The error message.</returns>
        private static string GetErrorMessage(Type declaringType, string handlerMethodName)
        {
            return $"'{declaringType.Name}.{handlerMethodName}' is an instance method, but message handler methods must be static!";
        }
    }

    /// <summary>The exception that is thrown when a method with a <see cref="EmbeddedMessageHandlerAttribute"/> does not have an acceptable message handler method signature (either <see cref="EmbeddedServer.MessageHandler"/> or <see cref="EmbeddedClient.MessageHandler"/>).</summary>
    public class InvalidHandlerSignatureException : Exception
    {
        /// <summary>The type containing the handler method.</summary>
        public readonly Type DeclaringType;
        /// <summary>The name of the handler method.</summary>
        public readonly string HandlerMethodName;

        /// <summary>Initializes a new <see cref="InvalidHandlerSignatureException"/> instance.</summary>
        public InvalidHandlerSignatureException() { }
        /// <summary>Initializes a new <see cref="InvalidHandlerSignatureException"/> instance with a specified error message.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public InvalidHandlerSignatureException(string message) : base(message) { }
        /// <summary>Initializes a new <see cref="InvalidHandlerSignatureException"/> instance with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception. If <paramref name="inner"/> is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public InvalidHandlerSignatureException(string message, Exception inner) : base(message, inner) { }
        /// <summary>Initializes a new <see cref="InvalidHandlerSignatureException"/> instance and constructs an error message from the given information.</summary>
        /// <param name="declaringType">The type containing the handler method.</param>
        /// <param name="handlerMethodName">The name of the handler method.</param>
        public InvalidHandlerSignatureException(Type declaringType, string handlerMethodName) : base(GetErrorMessage(declaringType, handlerMethodName))
        {
            DeclaringType = declaringType;
            HandlerMethodName = handlerMethodName;
        }

        /// <summary>Constructs the error message from the given information.</summary>
        /// <returns>The error message.</returns>
        private static string GetErrorMessage(Type declaringType, string handlerMethodName)
        {
            return $"'{declaringType.Name}.{handlerMethodName}' doesn't match any acceptable message handler method signatures! Server message handler methods should have a 'ushort' and a '{nameof(com.onlineobject.objectnet.embedded.EmbeddedMessage)}' parameter, while client message handler methods should only have a '{nameof(com.onlineobject.objectnet.embedded.EmbeddedMessage)}' parameter.";
        }
    }

    /// <summary>The exception that is thrown when multiple methods with <see cref="EmbeddedMessageHandlerAttribute"/>s are set to handle messages with the same ID <i>and</i> have the same method signature.</summary>
    public class DuplicateHandlerException : Exception
    {
        /// <summary>The message ID with multiple handler methods.</summary>
        public readonly ushort Id;
        /// <summary>The type containing the first handler method.</summary>
        public readonly Type DeclaringType1;
        /// <summary>The name of the first handler method.</summary>
        public readonly string HandlerMethodName1;
        /// <summary>The type containing the second handler method.</summary>
        public readonly Type DeclaringType2;
        /// <summary>The name of the second handler method.</summary>
        public readonly string HandlerMethodName2;

        /// <summary>Initializes a new <see cref="DuplicateHandlerException"/> instance with a specified error message.</summary>
        public DuplicateHandlerException() { }
        /// <summary>Initializes a new <see cref="DuplicateHandlerException"/> instance with a specified error message.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DuplicateHandlerException(string message) : base(message) { }
        /// <summary>Initializes a new <see cref="DuplicateHandlerException"/> instance with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception. If <paramref name="inner"/> is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public DuplicateHandlerException(string message, Exception inner) : base(message, inner) { }
        /// <summary>Initializes a new <see cref="DuplicateHandlerException"/> instance and constructs an error message from the given information.</summary>
        /// <param name="id">The message ID with multiple handler methods.</param>
        /// <param name="method1">The first handler method's info.</param>
        /// <param name="method2">The second handler method's info.</param>
        public DuplicateHandlerException(ushort id, MethodInfo method1, MethodInfo method2) : base(GetErrorMessage(id, method1, method2))
        {
            Id = id;
            DeclaringType1 = method1.DeclaringType;
            HandlerMethodName1 = method1.Name;
            DeclaringType2 = method2.DeclaringType;
            HandlerMethodName2 = method2.Name;
        }

        /// <summary>Constructs the error message from the given information.</summary>
        /// <returns>The error message.</returns>
        private static string GetErrorMessage(ushort id, MethodInfo method1, MethodInfo method2)
        {
            return $"Message handler methods '{method1.DeclaringType.Name}.{method1.Name}' and '{method2.DeclaringType.Name}.{method2.Name}' are both set to handle messages with ID {id}! Only one handler method is allowed per message ID!";
        }
    }
}
