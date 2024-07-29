using System.Collections.Generic;
using UnityEngine;

namespace com.onlineobject.objectnet {

    /// <summary>
    /// Represents a network entity for transmitting and receiving position data.
    /// </summary>
    public class PositionNetwork : NetworkEntity<Vector3, IDataStream> {

        // Private fields for managing position and network settings
        private Vector3 position = Vector3.zero;
        private bool enableXAxis = true;
        private bool enableYAxis = true;
        private bool enableZAxis = true;
        private bool initialized = false;
        private bool useInterpolation = true;
        private bool usePrediction = false;
        private bool autoResync = true;
        private bool teleport = false;
        private float teleportTimeInterval = 0f;
        private PredictionType predictionType = PredictionType.Automatic;
        private PredictionType movementType = PredictionType.Automatic;
        private PositionReference positionReference = PositionReference.UseGlobal;
        private float receivedTime = 0f;
        private float previousTime = 0f;
        private Vector3 movementVelocity = Vector3.zero;
        private int currentLocalTick = 0;
        private int currentServerTick = 0;
        private bool currentTeleport = false;
        private float currentDeltaTime = 0f;
        private float previousExecutionTime = 0f;
        private float executionDeltaTime = 0f;
        private Vector3 currentVelocity = Vector3.zero;
        private float timeToReachTarget = 0.0f;
        private float movementThreshold = 0.05f;
        private bool interpolateInitialized = false;
        private float lastSendTime = 0f;
        private Rigidbody physicsBody;
        private Rigidbody2D physicsBody2D;
        private Transform transformElement;
        private Vector3 previousPositionForVelocity;
        private readonly SortedList<int, PositionPacket> futurePositionUpdates = new SortedList<int, PositionPacket>();
        private float squaredMovementThreshold;
        private PositionPacket toPosition;
        private PositionPacket fromPosition;
        private PositionPacket previousPosition;
        private IPrediction predictionLogic;

        private int currentProcessedTickIndex = 0;
        private int previousProcessedTickIndex = 0;
        private int currentProcessedTick = 0;
        private int previousProcessedTick = 0;

        // Circular buffer for position history and time history
        Vector3[] positionrHistoryBuffer = new Vector3[CIRCULAR_BUFFER_SIZE];
        float[] timeHistoryBuffer = new float[CIRCULAR_BUFFER_SIZE];
        int currentPositionIndex = 0;

        // Constants for network latency and prediction settings
        const int CIRCULAR_BUFFER_SIZE = 1024;
        const float MAX_PREDICTION_FPS_PERCENT = 0.75f;
        const float MILLISECONDS_TIME_DIVISOR = 1000f;
        const int RANGE_GOOD_LATENCY = 2;
        const int RANGE_ACCEPTABLE_LATENCY = 6;
        const int MAX_RANGE_LATENCY_MULTIPLIER = 2;
        const int ZERO_LATENCY = 0;
        const int MAXIMUM_LATENCY = 140; // 140 ms
        const int MAXIMUN_OBJECT_SPEED = 40;
        const int MIN_PREDICTION_BUFFER = 10;
        const int MAX_PREDICTION_BUFFER = 120;
        const int DEFAULT_PREDICTION_BUFFER = 30;
        const float DISTANCE_THRESHOULD = 0.01f;
        const float TELEPORT_DURATION = 0.25f;
        /// <summary>
        // Default constructor
        /// <summary>
        public PositionNetwork() : base() {
        }

        /// <summary>
        /// Initializes a new instance of the PositionNetwork class with specified settings for axis synchronization and prediction/interpolation techniques.
        /// </summary>
        /// <param name="x">Enable synchronization on the X axis.</param>
        /// <param name="y">Enable synchronization on the Y axis.</param>
        /// <param name="z">Enable synchronization on the Z axis.</param>
        /// <param name="usePrediction">Use prediction for movement.</param>
        /// <param name="useInterpolation">Use interpolation for smoother movement.</param>
        /// <param name="predictionTechnique">The prediction technique to use.</param>
        public PositionNetwork(bool x, bool y, bool z, bool usePrediction = false, bool useInterpolation = true, PredictionType predictionTechnique = PredictionType.Automatic, PredictionType movementTechnique = PredictionType.Automatic, bool autoResync = true) : base() {
            this.enableXAxis = x;
            this.enableYAxis = y;
            this.enableZAxis = z;
            this.useInterpolation = useInterpolation;
            this.usePrediction = usePrediction;
            this.predictionType = predictionTechnique;
            this.movementType = movementTechnique;
            this.autoResync = autoResync;
            this.lastSendTime = NetworkClock.time;
        }

        /// <summary>
        /// Initializes a new instance of the PositionNetwork class with a reference to an existing network object.
        /// </summary>
        /// <param name="networkObject">The network object to synchronize.</param>
        public PositionNetwork(INetworkElement networkObject) : base(networkObject) {
        }

        /// <summary>
        /// Initializes interpolation data for the networked object.
        /// </summary>
        private void InitializeInterpolations() {
            this.squaredMovementThreshold = (this.movementThreshold * this.movementThreshold);
            // Register future position on packet
            this.futurePositionUpdates.Add(NetworkManager.Instance().RemoteTicks,        new PositionPacket(NetworkManager.Instance().RemoteTicks,     false, this.GetObjectPosition(), NetworkClock.fixedDeltaTime, Vector3.forward));
            this.futurePositionUpdates.Add(NetworkManager.Instance().InterpolationTicks, new PositionPacket(NetworkManager.Instance().RemoteTicks + 1, false, this.GetObjectPosition(), NetworkClock.fixedDeltaTime, Vector3.forward));
            
            // Initialize position packets for interpolation
            this.toPosition         = this.futurePositionUpdates[this.futurePositionUpdates.Keys[1]];
            this.fromPosition       = this.futurePositionUpdates[this.futurePositionUpdates.Keys[0]];
            this.previousPosition   = this.futurePositionUpdates[this.futurePositionUpdates.Keys[0]];
        }

        /// <summary>
        /// Computes the active state of the networked object, updating its position if it has moved beyond a threshold.
        /// </summary>
        public override void ComputeActive() {
            // Check if the object has moved beyond the threshold and flag it for update
            this.FlagUpdated(Vector3.Distance(this.position, this.GetObjectPosition()) > DISTANCE_THRESHOULD);
            // Update the current position
            this.position = this.GetObjectPosition();
        }

        /// <summary>
        /// Computes the passive state of the networked object, handling interpolation and prediction if necessary.
        /// </summary>
        public override void ComputePassive() {
            if (!this.initialized) {
                // Initialize the networked object's state
                this.initialized            = true;
                this.position               = this.GetObjectPosition();
                this.physicsBody            = this.GetNetworkObject().GetGameObject().GetComponent<Rigidbody>();
                this.physicsBody2D          = this.GetNetworkObject().GetGameObject().GetComponent<Rigidbody2D>();
                this.transformElement       = this.GetNetworkObject().GetGameObject().GetComponent<Transform>();
                this.previousExecutionTime  = NetworkClock.time;
                this.executionDeltaTime     = 0f;
            } else {
                // Calculate the time since the last execution
                this.executionDeltaTime     = (NetworkClock.time - this.previousExecutionTime);
                this.previousExecutionTime  = NetworkClock.time;
            }
            if (this.useInterpolation) {
                if (!this.interpolateInitialized) {
                    // Initialize interpolation if it hasn't been done yet
                    this.InitializeInterpolations();
                    this.interpolateInitialized = true;
                }
                // Process future position updates if needed
                if (this.autoResync) {
                    for (int index = 0; index < this.futurePositionUpdates.Keys.Count; index++) {
                        if (NetworkManager.Instance().RemoteTicks >= this.futurePositionUpdates[this.futurePositionUpdates.Keys[index]].Tick) {
                            PositionPacket originalPosition = this.futurePositionUpdates[this.futurePositionUpdates.Keys[index]];
                            PositionPacket adjustmentPositionPacket = new PositionPacket(originalPosition.Tick,
                                                                                         originalPosition.IsTeleport,
                                                                                         this.GetObjectPosition(),
                                                                                         originalPosition.DeltaTime,
                                                                                         originalPosition.Velocity);
                            // Replace by adjusted position
                            this.futurePositionUpdates[this.futurePositionUpdates.Keys[index]] = adjustmentPositionPacket;
                        }
                    }
                }
                this.previousProcessedTickIndex = this.currentProcessedTickIndex;
                this.currentProcessedTickIndex = Mathf.Clamp(this.currentProcessedTickIndex + 1,
                                                              0,
                                                              this.futurePositionUpdates.Count - 1);
                if (this.currentProcessedTickIndex == this.previousProcessedTickIndex) {
                    if (this.futurePositionUpdates.Count > 1) {
                        this.previousProcessedTickIndex = this.currentProcessedTickIndex - 1;
                    }
                }

                if ((NetworkManager.Instance().IsRemoteTicksInitialized) &&
                    (this.currentProcessedTickIndex > 1) &&
                    (this.previousProcessedTickIndex > 1) &&
                    (this.currentProcessedTickIndex < this.futurePositionUpdates.Count) &&
                    (this.previousProcessedTickIndex < this.futurePositionUpdates.Count)) {
                    this.fromPosition = this.futurePositionUpdates[this.futurePositionUpdates.Keys[this.previousProcessedTickIndex - 1]];
                    this.previousPosition = this.fromPosition;
                    this.toPosition = this.futurePositionUpdates[this.futurePositionUpdates.Keys[this.currentProcessedTickIndex - 1]];

                    this.currentProcessedTick = this.futurePositionUpdates.Keys[this.previousProcessedTickIndex - 1];
                    this.previousProcessedTick = this.futurePositionUpdates.Keys[this.currentProcessedTickIndex - 1];
                }
                // On teleports i need to jump directly to the new position
                if (this.toPosition.IsTeleport) {
                    this.UpdateObjectPosition(this.toPosition.Position);
                } else {
                    // Increase ellapsed time to reach
                    Vector3 positionToReach = (this.usePrediction) ? this.GetPositionInFuture(this.toPosition.Position,
                                                                                              this.toPosition.Velocity,
                                                                                              this.toPosition.DeltaTime,
                                                                                              NetworkManager.Instance().GetCurrentFPS()) : this.toPosition.Position;

                    // Interpolate or predict the position and move the object
                    if (PredictionType.UseTransform.Equals(this.DetectMovementType())) {
                        this.UpdateObjectPosition(Vector3.MoveTowards(this.GetNetworkObject().GetGameObject().transform.position,
                                                                 Vector3.SmoothDamp(this.GetNetworkObject().GetGameObject().transform.position,
                                                                                    positionToReach,
                                                                                    ref this.movementVelocity,
                                                                                    this.toPosition.DeltaTime),
                                                                 Vector3.Distance(this.GetNetworkObject().GetGameObject().transform.position, positionToReach) / this.toPosition.DeltaTime));
                    } else if (PredictionType.UsePhysics.Equals(this.DetectMovementType())) {
                        this.physicsBody.MovePosition(Vector3.MoveTowards(this.GetObjectPosition(),
                                                                          Vector3.SmoothDamp(this.GetObjectPosition(),
                                                                                            positionToReach,
                                                                                            ref this.movementVelocity,
                                                                                            this.toPosition.DeltaTime),
                                                                          Vector3.Distance(this.GetObjectPosition(), positionToReach) / this.toPosition.DeltaTime));
                    }
                }
            } else if (this.currentTeleport == true) {
                this.UpdateObjectPosition(this.position); // On teleports i need to jump directly to the new position
            } else { 
                if (PredictionType.UseTransform.Equals(this.DetectMovementType())) {
                    this.UpdateObjectPosition(Vector3.MoveTowards(this.GetNetworkObject().GetGameObject().transform.position,
                                         Vector3.SmoothDamp(this.GetNetworkObject().GetGameObject().transform.position,
                                                            this.position,
                                                            ref this.movementVelocity,
                                                            Mathf.Abs(this.previousTime - this.receivedTime)),
                                         Vector3.Distance(this.GetObjectPosition(), this.position) / Mathf.Abs(this.previousTime - this.receivedTime)));
                } else if (PredictionType.UsePhysics.Equals(this.DetectMovementType())) {
                    this.physicsBody.MovePosition(Vector3.MoveTowards(this.GetObjectPosition(),
                                                                      Vector3.SmoothDamp(this.GetObjectPosition(),
                                                                                        this.position,
                                                                                        ref this.movementVelocity,
                                                                                        Mathf.Abs(this.previousTime - this.receivedTime) * NetworkClock.fixedDeltaTime),
                                                                      Vector3.Distance(this.GetObjectPosition(), this.position) / Mathf.Abs(this.previousTime - this.receivedTime) * NetworkClock.fixedDeltaTime));
                }
            }
        }

        /// <summary>
        /// Synchronizes the active state of the object with the network by writing necessary data to the provided data stream.
        /// </summary>
        /// <param name="writer">The data stream to write synchronization data to.</param>
        public override void SynchonizeActive(IDataStream writer) {
            // Initialize the object's state if it hasn't been initialized yet.
            if (!this.initialized) {
                this.initialized = true;
                // Cache components and initial state for later use.
                this.position                   = this.GetObjectPosition();
                this.physicsBody                = this.GetNetworkObject().GetGameObject().GetComponent<Rigidbody>();
                this.physicsBody2D              = this.GetNetworkObject().GetGameObject().GetComponent<Rigidbody2D>();
                this.transformElement           = this.GetNetworkObject().GetGameObject().GetComponent<Transform>();
                this.previousExecutionTime      = NetworkClock.time;
                this.executionDeltaTime         = 0f;
                if (this.physicsBody != null) {
                    this.physicsBody.interpolation = RigidbodyInterpolation.Interpolate; // Flag to interpolate physics
                }
            } else {
                // Update the execution delta time.
                this.executionDeltaTime = (NetworkClock.time - this.previousExecutionTime);
                this.previousExecutionTime = NetworkClock.time;
            }
            // Write if update is paused
            writer.Write(this.IsPaused());
            if (this.IsPaused() == false) {
                // Write the current server tick and teleportation flag to the data stream.
                writer.Write(NetworkManager.Instance().LocalTicks);
                writer.Write(this.teleport); // Placeholder for teleportation flag (requires implementation).
                writer.Write((byte)this.positionReference); // Write how position is oriented  
                // Write additional prediction data if prediction is enabled.
                if (this.usePrediction) {
                    writer.Write(NetworkClock.time - this.lastSendTime);
                    writer.Write(this.GetObjectVelocity());
                }
                // Write position data based on enabled axes.
                if (this.enableXAxis && this.enableYAxis && this.enableZAxis) {
                    writer.Write(this.position);
                } else {
                    if (this.enableXAxis)
                        writer.Write(this.position.x);
                    if (this.enableYAxis)
                        writer.Write(this.position.y);
                    if (this.enableZAxis)
                        writer.Write(this.position.z);
                }
                // Disable teleport time after time is finished
                this.teleport = this.teleport & (this.teleportTimeInterval > NetworkClock.time);
            }
            // Register the current position for time-based operations.
            this.RegisterPositionOnTime(this.position, NetworkClock.time, ++this.currentPositionIndex);
            // Flag last send time 
            this.lastSendTime = NetworkClock.time;
        }

        /// <summary>
        /// Synchronizes the passive state of the object by updating its position.
        /// </summary>
        /// <param name="data">The new position data to apply to the object.</param>
        public override void SynchonizePassive(Vector3 data) {
            this.position = data;
        }

        /// <summary>
        /// Retrieves the current position of the object for passive synchronization.
        /// </summary>
        /// <returns>The current position of the object.</returns>
        public override Vector3 GetPassiveArguments() {
            return this.position;
        }

        /// <summary>
        /// Extracts synchronization data from the provided data stream and updates the object's state accordingly.
        /// </summary>
        /// <param name="reader">The data stream to read synchronization data from.</param>
        public override void Extract(IDataStream reader) {
            // Update timing information and start reading data.
            this.previousTime   = (this.receivedTime > 0) ? this.receivedTime : NetworkClock.time;
            this.receivedTime   = NetworkClock.time;
            // First extract if position is paused by other side
            bool isSenderPaused = reader.Read<bool>();
            if (isSenderPaused == false) {
                this.Resume();
                // Read synchronization data from the data stream.
                this.currentServerTick = reader.Read<int>();
                this.currentTeleport = reader.Read<bool>();
                this.positionReference = ((PositionReference)reader.Read<byte>()); // Update position according to the 
                if (this.usePrediction) {
                    this.currentDeltaTime   = reader.Read<float>();
                    this.currentVelocity    = reader.Read<Vector3>();
                }
                // Read position data based on enabled axes.
                if (this.enableXAxis && this.enableYAxis && this.enableZAxis) {
                    this.position = reader.Read<Vector3>();
                } else {
                    this.position = new Vector3(
                        (this.enableXAxis) ? reader.Read<float>() : this.GetObjectPosition().x,
                        (this.enableYAxis) ? reader.Read<float>() : this.GetObjectPosition().y,
                        (this.enableZAxis) ? reader.Read<float>() : this.GetObjectPosition().z
                    );
                }
                // If is to teleport i need to remove all pending updated from queue
                if ( this.currentTeleport == true ) {
                    // Keep on message on queue to perform movement
                    while (this.futurePositionUpdates.Count > 1) {
                        this.futurePositionUpdates.RemoveAt(0);
                    }
                    // Fix teh current and previous tick position
                    if (this.futurePositionUpdates.Count > 0) {
                        this.previousProcessedTickIndex = this.futurePositionUpdates.Keys[0];
                        this.currentProcessedTickIndex  = this.futurePositionUpdates.Keys[0];
                    } else {
                        this.previousProcessedTickIndex = this.currentServerTick;
                        this.currentProcessedTickIndex  = this.currentServerTick;
                    }
                }
                // Initialize interpolation if necessary and register the received position.
                if (this.useInterpolation) {
                    if (!this.interpolateInitialized) {
                        this.InitializeInterpolations();
                        this.interpolateInitialized = true;
                    }
                    this.RegisterReceivedPosition(this.currentServerTick, this.currentTeleport, this.position, this.currentDeltaTime, this.currentVelocity);
                }
            } else {
                // When paused i need to clear values to not teleport back
                this.futurePositionUpdates.Clear();
                this.interpolateInitialized = false;
                this.Pause();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private PredictionType DetectMovementType() {
            PredictionType result = PredictionType.UseTransform;
            // Calculate velocity based on the prediction type and available components.
            if (PredictionType.Automatic.Equals(this.movementType)) {
                if ((this.physicsBody != null) || (this.physicsBody2D != null)) {
                    if (this.physicsBody != null) {
                        if (this.physicsBody.isKinematic) {
                            result = PredictionType.UseTransform;
                        } else {
                            result = PredictionType.UsePhysics;
                        }
                    } else if (this.physicsBody2D != null) {
                        if (this.physicsBody2D.isKinematic) {
                            result = PredictionType.UseTransform;
                        } else {
                            result = PredictionType.UsePhysics;
                        }
                    }
                } else {
                    result = PredictionType.UseTransform;
                }
            } else if (PredictionType.UsePhysics.Equals(this.movementType)) {
                if (this.physicsBody != null) {
                    if (this.physicsBody.isKinematic) {
                        result = PredictionType.UseTransform;
                    } else {
                        result = PredictionType.UsePhysics;
                    }
                }
            } else if (PredictionType.UseTransform.Equals(this.movementType)) {
                if (this.transformElement != null) {
                    result = PredictionType.UseTransform;
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates the linear velocity of the object based on its current state and prediction settings.
        /// </summary>
        /// <returns>The calculated linear velocity of the object.</returns>
        private Vector3 GetObjectVelocity() {
            Vector3 result = ((((PositionReference.UseGlobal.Equals(this.positionReference)) ? this.transformElement.position : this.transformElement.localPosition) - this.previousPositionForVelocity) / this.executionDeltaTime);
            this.previousPositionForVelocity = ((PositionReference.UseGlobal.Equals(this.positionReference)) ? this.transformElement.position : this.transformElement.localPosition);
            return result;
        }

        /// <summary>
        /// Registers a received position update for future processing, ensuring the updates are ordered by tick count.
        /// </summary>
        /// <param name="tick">The tick count associated with the position update.</param>
        /// <param name="teleport">A flag indicating whether the update involves teleportation.</param>
        /// <param name="position">The position data received.</param>
        /// <param name="deltaTime">The time delta associated with the position update.</param>
        /// <param name="velocity">The linear velocity associated with the position update.</param>
        private void RegisterReceivedPosition(int tick, bool teleport, Vector3 position, float deltaTime, Vector3 velocity) {
            // Add the position packet to the end if it's the latest tick.
            if (this.futurePositionUpdates.ContainsKey(tick))
                this.futurePositionUpdates[tick] = new PositionPacket(tick, teleport, position, deltaTime, velocity);
            else 
                this.futurePositionUpdates.Add(tick, new PositionPacket(tick, teleport, position, deltaTime, velocity));
            // Ensure the buffer size does not exceed the defined limits.
            if (this.futurePositionUpdates.Count > ((this.usePrediction) ? Mathf.Clamp(NetworkManager.Instance().GetPredictionBufferSize(), MIN_PREDICTION_BUFFER, MAX_PREDICTION_BUFFER) : DEFAULT_PREDICTION_BUFFER)) {
                // this.futurePositionUpdates.RemoveAt(0);
                this.futurePositionUpdates.Remove(this.futurePositionUpdates.Keys[0]);
            }
            // Register the position with the prediction logic if available.
            if (this.predictionLogic != null) {
                this.predictionLogic.RegisterPosition(position, deltaTime, velocity, teleport);
            }
        }

        /// <summary>
        /// Predicts the future position of the object based on its current state and network conditions.
        /// </summary>
        /// <param name="positionInFuture">The reference position to predict from.</param>
        /// <param name="velocity">The linear velocity of the object.</param>
        /// <param name="deltaTime">The time delta for the prediction.</param>
        /// <param name="currentFPS">The current frames per second of the simulation.</param>
        /// <returns>The predicted future position of the object.</returns>
        private Vector3 GetPositionInFuture(Vector3 positionInFuture, Vector3 velocity, float deltaTime, int currentFPS) {
            Vector3 resultPosition = positionInFuture;
            // Predict future position based on the absence of a custom prediction logic.
            if (this.predictionLogic == null) {
                // Calculate the average time delta and the number of frames needed for interpolation.
                float deltaAverage = deltaTime;
                float neededFrames = (deltaAverage / (1.0f / currentFPS));
                if (neededFrames > 0f) {
                    // Calculate the time frames into the future and the future position.
                    float timeFramesInFuture = ((1.0f / neededFrames) / currentFPS);
                    Vector3 futurePosition = positionInFuture;
                    Vector3 bodyVelocity = velocity;
                    // Calculate the time into the future based on network latency and other factors.
                    float timeIntoTheFuture = NetworkManager.Instance().GetPingAverage();
                    if (timeIntoTheFuture > 0f) {
                        // Apply various factors to the time into the future and calculate the final future position.
                        timeIntoTheFuture = (timeIntoTheFuture > 1f) ? Mathf.Sqrt(timeIntoTheFuture) : Mathf.Pow(timeIntoTheFuture, 2f);
                        timeIntoTheFuture = Mathf.Clamp(timeIntoTheFuture, 0f, NetworkManager.Instance().GetAcceptableLatencyValue());
                        timeIntoTheFuture = NetworkManager.Instance().GetPredictionFactor().Evaluate(timeIntoTheFuture / NetworkManager.Instance().GetAcceptableLatencyValue());
                        timeIntoTheFuture *= NetworkManager.Instance().GetPredictionSpeedFactor().Evaluate(Mathf.Clamp(bodyVelocity.magnitude, 0f, MAXIMUN_OBJECT_SPEED) / MAXIMUN_OBJECT_SPEED);
                        timeIntoTheFuture = Mathf.Clamp(timeIntoTheFuture, 0f, (1.0f / NetworkManager.Instance().GetCurrentFPS()) * (NetworkManager.Instance().GetCurrentFPS() * MAX_PREDICTION_FPS_PERCENT));
                        futurePosition += (bodyVelocity * timeIntoTheFuture);
                        futurePosition = Vector3.LerpUnclamped(positionInFuture, futurePosition, 1.0f + timeFramesInFuture);
                        resultPosition = futurePosition;
                    }
                }
            } else {
                // Use custom prediction logic if available.
                resultPosition = this.predictionLogic.Predict(positionInFuture, velocity, deltaTime, currentFPS);
            }
            return resultPosition;
        }


        /// <summary>
        /// Return if this object was closer to this position on past
        /// </summary>
        /// <param name="positionToCheck">Position to be checked</param>
        /// <param name="exactlyMatch">If true only will accept if position matched at exactly time in past, when false any value during travel past will be accepted</param>
        /// <returns></returns>
        public bool IsCorrectlyPositionOnPast(Vector3 positionToCheck, bool exactlyMatch = true) {
            return this.IsCorrectlyPositionOnPast(this.currentPositionIndex, 
                                                  NetworkManager.Instance().GetPingAverage(), 
                                                  positionToCheck, 
                                                  RANGE_GOOD_LATENCY, 
                                                  RANGE_ACCEPTABLE_LATENCY,
                                                  exactlyMatch);
        }

        /// <summary>
        /// Return if this object was int os closer to this position on past
        /// </summary>
        /// <param name="positionToCheck">Position to be checked</param>
        /// <param name="rangeOnGoodLatency">How long in past must be checked for good latency ( steps in time )</param>
        /// <param name="rangeOnTolerableLatency">How long in past must be checked for acceptable latency ( steps in time )</param>
        /// <param name="exactlyMatch">If true only will accept if position matched at exactly time in past, when false any value during travel past will be accepted</param>
        /// <returns></returns>
        public bool IsCorrectlyPositionOnPast(Vector3 positionToCheck, int rangeOnGoodLatency = RANGE_GOOD_LATENCY, int rangeOnTolerableLatency = RANGE_ACCEPTABLE_LATENCY, bool exactlyMatch = true) {
            return this.IsCorrectlyPositionOnPast(this.currentPositionIndex, 
                                                  NetworkManager.Instance().GetPingAverage(), 
                                                  positionToCheck, 
                                                  rangeOnGoodLatency, 
                                                  rangeOnTolerableLatency,
                                                  exactlyMatch);
        }

        /// <summary>
        /// Determines if the position is correctly placed within a time frame based on latency.
        /// </summary>
        /// <param name="startIndexOnTime">The starting index on the time buffer.</param>
        /// <param name="latency">The latency in seconds.</param>
        /// <param name="position">The position to check.</param>
        /// <param name="rangeOnGoodLatency">The range considered for good latency.</param>
        /// <param name="rangeOnToolerableLatency">The range considered for tolerable latency.</param>
        /// <param name="exactlyMatch">Whether to exactly match the position or not.</param>
        /// <returns>True if the position is correctly placed, otherwise false.</returns>
        private bool IsCorrectlyPositionOnPast(int startIndexOnTime, float latency, Vector3 position, int rangeOnGoodLatency = RANGE_GOOD_LATENCY, int rangeOnToolerableLatency = RANGE_ACCEPTABLE_LATENCY, bool exactlyMatch = true) {
            bool result = false;
            int MIN_FRAME_RANGE = ((latency * MILLISECONDS_TIME_DIVISOR) <= NetworkManager.Instance().GetGoodLatencyValue()) ? rangeOnGoodLatency : (rangeOnGoodLatency * MAX_RANGE_LATENCY_MULTIPLIER);
            int MAX_FRAME_RANGE = ((latency * MILLISECONDS_TIME_DIVISOR) <= NetworkManager.Instance().GetGoodLatencyValue()) ? rangeOnToolerableLatency : ( rangeOnToolerableLatency * MAX_RANGE_LATENCY_MULTIPLIER );
            // Navigate back to check if data matches with recorded on time machine
            float acceptableLatency         = Mathf.Clamp((latency > 1f) ? Mathf.Sqrt(latency) : Mathf.Pow(latency, 2f), ZERO_LATENCY, MAXIMUM_LATENCY /MILLISECONDS_TIME_DIVISOR );
            int frameRange                  = Mathf.Clamp((startIndexOnTime - this.RecoverFrameTimeOnPast(acceptableLatency, startIndexOnTime)), 0, MAX_FRAME_RANGE);
            int frameRangeToUse             = Mathf.Clamp(frameRange, MIN_FRAME_RANGE, MAX_FRAME_RANGE);
            // Get back position on time
            if ( exactlyMatch ) {
                Vector3 previousPositionOnEvent = this.RecoverPositionOnTime(startIndexOnTime - frameRangeToUse);
                Vector3 positionOnEvent         = this.RecoverPositionOnTime(startIndexOnTime);
                Vector3 nextPositionOnEvent     = this.RecoverPositionOnTime(startIndexOnTime + frameRangeToUse);
                float   sphereRadius            = (Vector3.Distance(previousPositionOnEvent, nextPositionOnEvent) / 2.0f);            
                result                          = (this.IsPointInsideSphere(positionOnEvent, sphereRadius, position, 0f) == true); 
            } else {
                int indexToCheck = frameRangeToUse;
                while ((result == false) && (indexToCheck > 0)) {
                    Vector3 previousPositionOnEvent = this.RecoverPositionOnTime(startIndexOnTime - indexToCheck);
                    Vector3 positionOnEvent         = this.RecoverPositionOnTime(startIndexOnTime);
                    Vector3 nextPositionOnEvent     = this.RecoverPositionOnTime(startIndexOnTime + indexToCheck);
                    float   sphereRadius            = (Vector3.Distance(previousPositionOnEvent, nextPositionOnEvent) / 2.0f);            
                    result                          = (this.IsPointInsideSphere(positionOnEvent, sphereRadius, position, 0f) == true);
                    indexToCheck--;
                }
            }
            return result;
        }

        /// <summary>
        /// Checks if a given point is inside a sphere with a certain tolerance.
        /// </summary>
        /// <param name="sphereCenter">The center of the sphere.</param>
        /// <param name="sphereRadius">The radius of the sphere.</param>
        /// <param name="pointToCheck">The point to check.</param>
        /// <param name="tolerance">The tolerance for the check.</param>
        /// <returns>True if the point is inside the sphere, otherwise false.</returns>
        private bool IsPointInsideSphere(Vector3 sphereCenter, float sphereRadius, Vector3 pointToCheck, float tolerance = 0f) {
            float distanceFromCenter = (Vector3.Distance(sphereCenter, pointToCheck) - tolerance);
            return (distanceFromCenter <= sphereRadius);
        }

        /// <summary>
        /// Registers the position and time in a circular buffer based on the sequence index.
        /// </summary>
        /// <param name="registeredPosition">The position to register.</param>
        /// <param name="time">The time to register.</param>
        /// <param name="sequenceIndex">The sequence index for the circular buffer.</param>
        private void RegisterPositionOnTime(Vector3 registeredPosition, float time, int sequenceIndex) {
            this.positionrHistoryBuffer  [(sequenceIndex % CIRCULAR_BUFFER_SIZE)]  = new Vector3(registeredPosition.x, registeredPosition.y, registeredPosition.z);
            this.timeHistoryBuffer      [(sequenceIndex % CIRCULAR_BUFFER_SIZE)]  = time;
        }

        /// <summary>
        /// Recovers the position from the history buffer based on the sequence index.
        /// </summary>
        /// <param name="sequenceIndex">The sequence index for the circular buffer.</param>
        /// <returns>The position from the history buffer.</returns>
        private Vector3 RecoverPositionOnTime(int sequenceIndex) {
            return this.positionrHistoryBuffer[(sequenceIndex % CIRCULAR_BUFFER_SIZE)];
        }

        /// <summary>
        /// Recovers the time from the history buffer based on the sequence index.
        /// </summary>
        /// <param name="sequenceIndex">The sequence index for the circular buffer.</param>
        /// <returns>The time from the history buffer.</returns>
        private float RecoverTimeOnPast(int sequenceIndex) {
            return this.timeHistoryBuffer[(sequenceIndex % CIRCULAR_BUFFER_SIZE)];
        }

        /// <summary>
        /// Recovers the frame time from the past based on the time to go back and the start sequence.
        /// </summary>
        /// <param name="timeBack">The time to go back in the history.</param>
        /// <param name="startSequence">The starting sequence index.</param>
        /// <returns>The index of the frame time recovered from the past.</returns>
        public virtual int RecoverFrameTimeOnPast(float timeBack, int startSequence) {
            float   startTime       = 0f;
            float   endTime         = 0f;
            float   remaningTime    = timeBack;
            int     index           = startSequence;
            do {
                index--;
                startTime       = this.timeHistoryBuffer[((index - 1) % CIRCULAR_BUFFER_SIZE)];
                endTime         = this.timeHistoryBuffer[(index % CIRCULAR_BUFFER_SIZE)];
                remaningTime    -= Mathf.Abs(endTime - startTime);
            } while ((remaningTime > 0f) && (index > 0));
            return index;
        }

        /// <summary>
        /// Sets a custom prediction logic.
        /// </summary>
        /// <param name="prediction">The prediction logic to set.</param>
        public void SetCustomPrediction(IPrediction prediction) {
            this.predictionLogic = prediction;
        }

        /// <summary>
        /// Teleport network object to some position withou inteporlating his movement
        /// </summary>
        /// <param name="teleportPosition">Position to teleport object</param>
        /// <param name="teleportTime">Optional time duration of teleporting to ensure that object will be teleported</param>
        public void Teleport(Vector3 teleportPosition, float teleportTime = TELEPORT_DURATION) {
            this.teleport               = true;
            this.teleportTimeInterval   = (NetworkClock.time + teleportTime);
            this.position               = teleportPosition; // Update current syn position
            this.UpdateObjectPosition(teleportPosition); // Then move object to this position
            this.FlagUpdated(true); // Flag as updated to ensure tha will be sync
        }

        /// <summary>
        /// Define the type of position reference that system will use to this network object 
        /// </summary>
        /// <param name="reference">Reference type to use</param>
        public void UpdatePositionReference(PositionReference reference) {
            this.positionReference = reference;
        }
        
        /// <summary>
        /// Update object position using the configured position orientation as reference
        /// </summary>
        /// <param name="updatedPosition">Position to apply</param>
        private void UpdateObjectPosition(Vector3 updatedPosition) {
            if (PositionReference.UseGlobal.Equals(this.positionReference)) {
                this.GetNetworkObject().GetGameObject().transform.position = updatedPosition;
            } else if (PositionReference.UseLocal.Equals(this.positionReference)) {
                this.GetNetworkObject().GetGameObject().transform.localPosition = updatedPosition;
            }
        }
        
        /// <summary>
        /// Get current object position using position orientation as reference
        /// </summary>
        /// <returns>Current oriented position value</returns>
        private Vector3 GetObjectPosition() {
            if (PositionReference.UseGlobal.Equals(this.positionReference)) {
                return this.GetNetworkObject().GetGameObject().transform.position;
            } else if (PositionReference.UseLocal.Equals(this.positionReference)) {
                return this.GetNetworkObject().GetGameObject().transform.localPosition;
            } else {
                return this.GetNetworkObject().GetGameObject().transform.position;
            }
        }
    }
}