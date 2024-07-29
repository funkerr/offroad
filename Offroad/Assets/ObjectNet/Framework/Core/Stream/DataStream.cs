using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.onlineobject.objectnet {
    /// <summary>
    /// Represents a data stream that can handle various data types and network operations.
    /// </summary>
    public class DataStream : IDataStream, INetworkStream {

        // Buffer to hold the data stream.
        private byte[] dataBuffer;

        // Current position in the buffer.
        private int bufferIndex = 0;

        // Size of the buffer.
        private int bufferSize = 0;

        // Flag to determine if buffer allocation should be automatic.
        private bool autoAllocate = true;

        // Client associated with the data stream.
        private IClient client;

        // Mapping of data types to their respective read and write stream handlers.
        private Dictionary<Type, Tuple<IData, IData>> streamMap = new Dictionary<Type, Tuple<IData, IData>>();

        // Global factory for creating stream handlers based on data types.
        private static Dictionary<Type, Type> streamFactory = new Dictionary<Type, Type>();

        // Global allowed stream types
        private static List<Type> streamAllowedTypes = new List<Type>();

        // Default size for the buffer if not specified.
        const int DEFAULT_BUFFER_SIZE = 1024;

        /// <summary>
        /// Initializes a new instance of the DataStream class with a specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to use for the data stream.</param>
        public DataStream(byte[] buffer) {
            this.Initialize();
            this.bufferSize = 0;
            this.bufferIndex = 0;
            this.dataBuffer = buffer;
        }

        /// <summary>
        /// Initializes a new instance of the DataStream class with a specified client, buffer, and buffer size.
        /// </summary>
        /// <param name="client">The client associated with the data stream.</param>
        /// <param name="buffer">The buffer to use for the data stream.</param>
        /// <param name="bufferSize">The size of the buffer.</param>
        public DataStream(IClient client, byte[] buffer, int bufferSize = 0) {
            this.Initialize();
            this.bufferSize = bufferSize;
            this.bufferIndex = 0;
            this.dataBuffer = buffer;
            this.client = client;
        }

        /// <summary>
        /// Initializes a new instance of the DataStream class with default settings.
        /// </summary>
        public DataStream() {
            this.bufferIndex = 0;
            this.Initialize();
        }

        /// <summary>
        /// Registers a global stream handler for a specific data type.
        /// </summary>
        /// <typeparam name="T">The type of the stream handler.</typeparam>
        /// <param name="dataType">The data type to associate with the stream handler.</param>
        /// <param name="streamType">The stream handler type.</param>
        public static void RegisterGlobalStream<T>(Type dataType, T streamType) where T : Type {
            DataStream.streamFactory.Add(dataType, streamType); // Register on factory
            DataStream.streamAllowedTypes.Add(dataType); // Register into valid types
        }

        /// <summary>
        /// Initializes the data stream by setting up default stream handlers for common data types.
        /// </summary>
        private void Initialize() {
            this.streamMap.Add(typeof(Int16),       new Tuple<IData, IData>(new Int16Stream(),      new Int16Stream()));
            this.streamMap.Add(typeof(UInt16),      new Tuple<IData, IData>(new UInt16Stream(),     new UInt16Stream()));
            this.streamMap.Add(typeof(Int32),       new Tuple<IData, IData>(new Int32Stream(),      new Int32Stream()));
            this.streamMap.Add(typeof(UInt32),      new Tuple<IData, IData>(new UInt32Stream(),     new UInt32Stream()));
            this.streamMap.Add(typeof(float),       new Tuple<IData, IData>(new FloatStream(),      new FloatStream()));
            this.streamMap.Add(typeof(double),      new Tuple<IData, IData>(new DoubleStream(),     new DoubleStream()));
            this.streamMap.Add(typeof(bool),        new Tuple<IData, IData>(new BooleanStream(),    new BooleanStream()));
            this.streamMap.Add(typeof(string),      new Tuple<IData, IData>(new StringStream(),     new StringStream()));
            this.streamMap.Add(typeof(Color),       new Tuple<IData, IData>(new ColorStream(),      new ColorStream()));
            this.streamMap.Add(typeof(byte),        new Tuple<IData, IData>(new ByteStream(),       new ByteStream()));
            this.streamMap.Add(typeof(byte[]),      new Tuple<IData, IData>(new ByteArrayStream(),  new ByteArrayStream()));
            this.streamMap.Add(typeof(Vector2),     new Tuple<IData, IData>(new Vector2Stream(),    new Vector2Stream()));
            this.streamMap.Add(typeof(Vector3),     new Tuple<IData, IData>(new Vector3Stream(),    new Vector3Stream()));
            this.streamMap.Add(typeof(Quaternion),  new Tuple<IData, IData>(new QuaternionStream(), new QuaternionStream()));
            DataStream.streamAllowedTypes.Add(typeof(Int16));
            DataStream.streamAllowedTypes.Add(typeof(UInt16));
            DataStream.streamAllowedTypes.Add(typeof(Int32));
            DataStream.streamAllowedTypes.Add(typeof(UInt32));
            DataStream.streamAllowedTypes.Add(typeof(float));
            DataStream.streamAllowedTypes.Add(typeof(double));
            DataStream.streamAllowedTypes.Add(typeof(bool));
            DataStream.streamAllowedTypes.Add(typeof(string));
            DataStream.streamAllowedTypes.Add(typeof(Color));
            DataStream.streamAllowedTypes.Add(typeof(byte));
            DataStream.streamAllowedTypes.Add(typeof(byte[]));
            DataStream.streamAllowedTypes.Add(typeof(Vector2));
            DataStream.streamAllowedTypes.Add(typeof(Vector3));
            DataStream.streamAllowedTypes.Add(typeof(Quaternion));

            // Register global stream classes
            foreach (var streamFactory in DataStream.streamFactory) {
                this.streamMap.Add(streamFactory.Key, 
                                   new Tuple<IData, IData>((IData)Activator.CreateInstance(streamFactory.Value), 
                                                           (IData)Activator.CreateInstance(streamFactory.Value)));
            }
        }

        /// <summary>
        /// Registers a stream handler for a specific data type.
        /// </summary>
        /// <typeparam name="T">The type of the stream handler.</typeparam>
        /// <param name="dataType">The data type to associate with the stream handler.</param>
        /// <param name="stream">The stream handler instance.</param>
        public void RegisterStream<T>(Type dataType, T stream) where T : IData {
            this.streamMap.Add(dataType, new Tuple<IData, IData>(stream, stream));
        }

        /// <summary>
        /// Allocates a new buffer with a specified size.
        /// </summary>
        /// <param name="bufferSize">The size of the buffer to allocate.</param>
        public void Allocate(int bufferSize) {
            // Initialize a new byte array for the buffer with the given size
            this.dataBuffer = new byte[bufferSize];
            // Set the bufferSize field to the size of the new buffer
            this.bufferSize = bufferSize;
            // Reset the bufferIndex to 0, indicating the start of the buffer
            this.bufferIndex = 0;
        }

        /// <summary>
        /// Allocates a buffer using an existing byte array.
        /// </summary>
        /// <param name="buffer">The byte array to use as the buffer.</param>
        public void Allocate(byte[] buffer) {
            // Assign the provided byte array to the dataBuffer field
            this.dataBuffer = buffer;
            // Set the bufferSize field to the length of the buffer, or 0 if the buffer is null
            this.bufferSize = (buffer != null) ? buffer.Length : 0;
            // Reset the bufferIndex to 0, indicating the start of the buffer
            this.bufferIndex = 0;
        }

        /// <summary>
        /// Retrieves the current buffer.
        /// </summary>
        /// <returns>A copy of the current buffer as a byte array.</returns>
        public byte[] GetBuffer() {
            // Create a new byte array to hold the result
            byte[] result = new byte[this.bufferSize];
            // Copy the contents of the dataBuffer into the result array
            Array.Copy(this.dataBuffer, result, this.bufferSize);
            // Return the copied buffer
            return result;
        }

        /// <summary>
        /// Gets the current size of the buffer.
        /// </summary>
        /// <returns>The size of the buffer as an integer.</returns>
        public int GetBufferSize() {
            // Return the current index within the buffer, indicating the size of the used portion
            return this.bufferIndex;
        }

        /// <summary>
        /// Rewinds the buffer by a specified distance.
        /// </summary>
        /// <param name="distance">The distance to rewind within the buffer.</param>
        public void Rewind(int distance) {
            // Ensure the buffer is allocated before attempting to rewind
            this.AllocateBuffer();
            // Update the bufferIndex, clamping it to valid range to avoid out-of-bounds errors
            this.bufferIndex = Mathf.Clamp(this.bufferIndex - distance, 0, this.dataBuffer.Length - 1);
        }

        /// <summary>
        /// Rewinds the buffer by the size of a specified type.
        /// </summary>
        /// <typeparam name="T">The type used to determine the rewind distance.</typeparam>
        public void Rewind<T>() {
            // Ensure the buffer is allocated before attempting to rewind
            this.AllocateBuffer();
            // Update the bufferIndex based on the size of the type T, clamping it to valid range
            this.bufferIndex = Mathf.Clamp(this.bufferIndex - DataUtils.SizeOfType(typeof(T)), 0, this.dataBuffer.Length - 1);
        }

        /// <summary>
        /// Advances the buffer by a specified distance.
        /// </summary>
        /// <param name="distance">The distance to advance within the buffer.</param>
        public void Forward(int distance) {
            // Ensure the buffer is allocated before attempting to advance
            this.AllocateBuffer();
            // Update the bufferIndex, clamping it to valid range to avoid out-of-bounds errors
            this.bufferIndex = Mathf.Clamp(this.bufferIndex + distance, 0, this.dataBuffer.Length - 1);
        }

        /// <summary>
        /// Advances the buffer index forward by the size of the type T.
        /// </summary>
        /// <typeparam name="T">The type of data to calculate the size for advancing the buffer index.</typeparam>
        public void Forward<T>() {
            // Ensure the buffer is allocated before attempting to use it.
            this.AllocateBuffer();
            // Advance the buffer index by the size of type T, clamping it to ensure it doesn't exceed the buffer bounds.
            this.bufferIndex = Mathf.Clamp(this.bufferIndex + DataUtils.SizeOfType(typeof(T)), 0, this.dataBuffer.Length - 1);
        }

        /// <summary>
        /// Shifts the data in the buffer to the left starting from a specified index.
        /// </summary>
        /// <param name="start">The index from which to start shifting data.</param>
        /// <param name="distance">The number of positions to shift the data by.</param>
        public void ShiftLeft(int start, int distance) {
            // Ensure the buffer is allocated before attempting to use it.
            this.AllocateBuffer();
            // Shift the data in the buffer to the left by the specified distance.
            DataUtils.ShiftLeft(ref this.dataBuffer, start, distance);
            // Update the buffer index and size after the shift operation.
            this.bufferIndex -= distance;
            this.bufferSize = this.bufferIndex;
        }

        /// <summary>
        /// Shifts the data in the buffer to the right starting from a specified index.
        /// </summary>
        /// <param name="start">The index from which to start shifting data.</param>
        /// <param name="distance">The number of positions to shift the data by.</param>
        public void ShiftRight(int start, int distance) {
            // Ensure the buffer is allocated before attempting to use it.
            this.AllocateBuffer();
            // Shift the data in the buffer to the right by the specified distance.
            DataUtils.ShiftRight(ref this.dataBuffer, start, distance);
            // Update the buffer index and size after the shift operation.
            this.bufferIndex += distance;
            this.bufferSize = this.bufferIndex;
        }

        /// <summary>
        /// Reads data of type T from the buffer, with an option to rewind the buffer index before reading.
        /// </summary>
        /// <typeparam name="T">The type of data to read from the buffer.</typeparam>
        /// <param name="rewind">Whether to rewind the buffer index before reading.</param>
        /// <returns>The data read from the buffer.</returns>
        /// <exception cref="Exception">Thrown when the type T is not supported or does not have a reader.</exception>
        public T Read<T>(bool rewind = false) {
            // Initialize the result with the default value of type T.
            T result = default(T);
            // Check if there is a reader for type T in the stream map.
            IData reader = (this.streamMap.ContainsKey(typeof(T))) ? this.streamMap[typeof(T)].Item1 : null;
            if (reader != null) {
                // Check if the reader is of the correct data reader interface for type T.
                if (reader is IDataReader<T>) {
                    // Optionally rewind the buffer index before reading.
                    if (rewind) {
                        this.Rewind<T>();
                    }
                    // Read the data from the buffer using the reader.
                    result = (reader as IDataReader<T>).Read(this.dataBuffer, ref this.bufferIndex);
                } else {
                    // Throw an exception if the reader is not of the correct type.
                    throw new Exception(String.Format("[ {0} ] is not a reader", typeof(T)));
                }
            } else {
                // Throw an exception if there is no reader for type T.
                throw new Exception(String.Format("[ {0} ] is not supported", typeof(T)));
            }
            return result;
        }

        /// <summary>
        /// Reads data of type T from the buffer at a specific offset.
        /// </summary>
        /// <typeparam name="T">The type of data to read from the buffer.</typeparam>
        /// <param name="offset">The offset in the buffer from which to start reading.</param>
        /// <returns>The data read from the buffer.</returns>
        /// <exception cref="Exception">Thrown when the type T is not supported or does not have a reader.</exception>
        public T Read<T>(int offset) {
            // Initialize the result with the default value of type T.
            T result = default(T);
            // Check if there is a reader for type T in the stream map.
            IData reader = (this.streamMap.ContainsKey(typeof(T))) ? this.streamMap[typeof(T)].Item1 : null;
            if (reader != null) {
                // Check if the reader is of the correct data reader interface for type T.
                if (reader is IDataReader<T>) {
                    // Read the data from the buffer using the reader at the specified offset.
                    result = (reader as IDataReader<T>).Read(this.dataBuffer, ref offset);
                } else {
                    // Throw an exception if the reader is not of the correct type.
                    throw new Exception(String.Format("[ {0} ] is not a reader", typeof(T)));
                }
            } else {
                // Throw an exception if there is no reader for type T.
                throw new Exception(String.Format("[ {0} ] is not supported", typeof(T)));
            }
            return result;
        }


        /// <summary>
        /// Reads data of a specified type from the data buffer using a registered reader.
        /// </summary>
        /// <typeparam name="T">The type of data to read.</typeparam>
        /// <param name="dataType">The type of data to be read, used to find the appropriate reader.</param>
        /// <returns>The data read from the buffer.</returns>
        /// <exception cref="Exception">Thrown when the specified type is not supported or the reader is not found.</exception>
        public T Read<T>(Type dataType) {
            T result = default(T);
            // Attempt to retrieve the reader for the specified data type
            IData reader = (this.streamMap.ContainsKey(dataType)) ? this.streamMap[dataType].Item1 : null;
            if (reader != null) {
                // Check if the reader implements the IReader interface
                if (reader is IReader) {
                    // Use the reader to read the data from the buffer
                    result = (reader as IReader).Read<T>(this.dataBuffer, ref this.bufferIndex, dataType);
                } else {
                    // Throw an exception if the reader does not implement IReader
                    throw new Exception(String.Format("[ {0} ] is not a reader", dataType));
                }
            } else {
                // Throw an exception if no reader is registered for the data type
                throw new Exception(String.Format("[ {0} ] is not supported", dataType));
            }
            return result;
        }

        /// <summary>
        /// Reads data of a specified type from the stream.
        /// </summary>
        /// <param name="dataType">The type of the data to read.</param>
        /// <returns>The data read from the stream.</returns>
        public object Read(Type dataType) {
            object result = default(object);
            // Attempt to retrieve the reader for the specified data type
            IData reader = (this.streamMap.ContainsKey(dataType)) ? this.streamMap[dataType].Item1 : null;
            if (reader != null) {
                // Check if the reader implements the IReader interface
                if (reader is IReader) {
                    // Use the reader to read the data from the buffer
                    if (dataType == typeof(Int16))
                        result = (reader as IReader).Read<Int16>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(UInt16))
                        result = (reader as IReader).Read<UInt16>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(Int32))
                        result = (reader as IReader).Read<Int32>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(UInt32))
                        result = (reader as IReader).Read<UInt32>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(float))
                        result = (reader as IReader).Read<float>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(double))
                        result = (reader as IReader).Read<double>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(bool))
                        result = (reader as IReader).Read<bool>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(string))
                        result = (reader as IReader).Read<string>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(Color))
                        result = (reader as IReader).Read<Color>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(byte))
                        result = (reader as IReader).Read<byte>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(byte[]))
                        result = (reader as IReader).Read<byte[]>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(Vector2))
                        result = (reader as IReader).Read<Vector2>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(Vector3))
                        result = (reader as IReader).Read<Vector3>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else if (dataType == typeof(Quaternion))
                        result = (reader as IReader).Read<Quaternion>(this.dataBuffer, ref this.bufferIndex, dataType);
                    else
                        throw new Exception(String.Format("[ {0} ] is not a reader", dataType));
                } else {
                    // Throw an exception if the reader does not implement IReader
                    throw new Exception(String.Format("[ {0} ] is not a reader", dataType));
                }
            } else {
                // Throw an exception if no reader is registered for the data type
                throw new Exception(String.Format("[ {0} ] is not supported", dataType));
            }
            return result;
        }

        /// <summary>
        /// Resets the internal buffer index and size, and clears the data buffer.
        /// </summary>
        public void Reset() {
            this.bufferIndex = 0;
            this.bufferSize = 0;
            // Clear the data buffer if it is not null
            if (this.dataBuffer != null) {
                Array.Clear(this.dataBuffer, 0, this.dataBuffer.Length);
            }
        }

        /// <summary>
        /// Writes data of a specified type to the data buffer using a registered writer.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="data">The data to be written to the buffer.</param>
        /// <exception cref="Exception">Thrown when the specified type is not supported or the writer is not found.</exception>
        public void Write<T>(T data) {
            // Ensure the buffer is allocated before writing
            this.AllocateBuffer();
            // Attempt to retrieve the writer for the specified data type
            IData writter = (this.streamMap.ContainsKey(typeof(T))) ? this.streamMap[typeof(T)].Item2 : null;
            if (writter != null) {
                // Check if the writer implements the IDataWritter<T> interface
                if (writter is IDataWritter<T>) {
                    // Use the writer to write the data to the buffer and update the buffer index and size
                    this.bufferIndex += (writter as IDataWritter<T>).Write(data, ref this.dataBuffer, ref this.bufferIndex);
                    this.bufferSize = this.bufferIndex;
                } else {
                    // Throw an exception if the writer does not implement IDataWritter<T>
                    throw new Exception(String.Format("[ {0} ] is not a writter", typeof(T)));
                }
            } else {
                // Throw an exception if no writer is registered for the data type
                throw new Exception(String.Format("[ {0} ] is not supported", typeof(T)));
            }
        }

        /// <summary>
        /// Writes data of a specified type to the data buffer at a specific offset using a registered writer.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="data">The data to be written to the buffer.</param>
        /// <param name="offset">The offset at which to start writing the data.</param>
        /// <exception cref="Exception">Thrown when the specified type is not supported or the writer is not found.</exception>
        public void Write<T>(T data, int offset) {
            // Ensure the buffer is allocated before writing
            this.AllocateBuffer();
            // Attempt to retrieve the writer for the specified data type
            IData writter = (this.streamMap.ContainsKey(typeof(T))) ? this.streamMap[typeof(T)].Item2 : null;
            if (writter != null) {
                // Check if the writer implements the IDataWritter<T> interface
                if (writter is IDataWritter<T>) {
                    // Use the writer to write the data to the buffer at the specified offset
                    (writter as IDataWritter<T>).Write(data, ref this.dataBuffer, ref offset);
                } else {
                    // Throw an exception if the writer does not implement IDataWritter<T>
                    throw new Exception(String.Format("[ {0} ] is not a writter", typeof(T)));
                }
            } else {
                // Throw an exception if no writer is registered for the data type
                throw new Exception(String.Format("[ {0} ] is not supported", typeof(T)));
            }
        }

        /// <summary>
        /// Writes data of a specified type to the data buffer using a registered writer.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="data">The data to be written to the buffer.</param>
        /// <param name="dataType">The type of data to be written, used to find the appropriate writer.</param>
        /// <exception cref="Exception">Thrown when the specified type is not supported or the writer is not found.</exception>
        public void Write<T>(T data, Type dataType) {
            // Ensure the buffer is allocated before writing
            this.AllocateBuffer();
            // Attempt to retrieve the writer for the specified data type
            IData writter = (this.streamMap.ContainsKey(dataType)) ? this.streamMap[dataType].Item2 : null;
            if (writter != null) {
                // Check if the writer implements the IWritter interface
                if (writter is IWritter) {
                    // Use the writer to write the data to the buffer and update the buffer index and size
                    this.bufferIndex += (writter as IWritter).Write(data, ref this.dataBuffer, ref this.bufferIndex, dataType);
                    this.bufferSize = this.bufferIndex;
                } else {
                    // Throw an exception if the writer does not implement IWritter
                    throw new Exception(String.Format("[ {0} ] is not a writter", dataType));
                }
            } else {
                // Throw an exception if no writer is registered for the data type
                throw new Exception(String.Format("[ {0} ] is not supported", dataType));
            }
        }

        /// <summary>
        /// Return this this type is a valid Stream type
        /// </summary>
        /// <typeparam name="T">The type of data to check.</typeparam>
        /// <returns>true is a valid stream type, false otherwise</returns>
        public bool IsValidType<T>() {
            return this.streamMap.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Return this this type is a valid Stream type
        /// </summary>
        /// <param name="dataType">The type of data to check.</param>
        /// <returns>true is a valid stream type, false otherwise</returns>
        public bool IsValidType(Type dataType) {
            return DataStream.streamAllowedTypes.Contains(dataType);
        }

        /// <summary>
        /// Return associated type on index
        /// </summary>
        /// <param name="index">Index type</param>
        /// <returns>Type on specified index</returns>
        public Type GetType(ushort index) {
            return DataStream.streamAllowedTypes[index];
        }

        /// <summary>
        /// Return index of type on factory map
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>Index of specified type</returns>
        public ushort GetTypeIndex(Type type) {
            return (ushort)DataStream.streamAllowedTypes.IndexOf(type);
        }

        /// <summary>
        /// Return index of type on factory map
        /// </summary>
        /// <typeparam name="T">Type to check</typeparam>
        /// <returns>Index of specified type</returns>
        public ushort GetTypeIndex<T>() {
            return (ushort)DataStream.streamAllowedTypes.IndexOf(typeof(T));
        }

        /// <summary>
        /// Allocates the data buffer if automatic allocation is enabled and the buffer is not already allocated.
        /// </summary>
        private void AllocateBuffer() {
            if (this.autoAllocate) {
                if (this.dataBuffer == null) {
                    // Allocate the buffer with the default size
                    this.Allocate(DEFAULT_BUFFER_SIZE); // TODO: get buffer from a pooling and release on dispose
                }
            }
        }

        /// <summary>
        /// Disposes of the resources used by the instance, clearing the stream map and suppressing finalization.
        /// </summary>
        public void Dispose() {
            this.streamMap.Clear();
            // Suppress finalization to prevent the garbage collector from calling the finalizer
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Retrieves the client associated with this instance.
        /// </summary>
        /// <returns>The client associated with this instance.</returns>
        public IClient GetClient() {
            return this.client;
        }

    }

}