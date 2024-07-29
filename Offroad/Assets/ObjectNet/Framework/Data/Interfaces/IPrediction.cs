using UnityEngine;

namespace com.onlineobject.objectnet {

    /// <summary>
    /// Interface representing a possible predicted information
    /// </summary>
    public interface IPrediction {

        /// <summary>
        /// Predict position of object
        /// </summary>
        /// <param name="nextPosition">Next position on buffered positions to interpolate</param>
        /// <param name="linearVelocity">Linear velocity of object on origin</param>
        /// <param name="deltaTime">Delta time on origin</param>
        /// <param name="currentFPS">Current FPS where login is being executed</param>
        /// <returns></returns>
        Vector3 Predict(Vector3 nextPosition, Vector3 linearVelocity, float deltaTime, int currentFPS);

        /// <summary>
        /// Register received position
        /// </summary>
        /// <param name="position">Received position</param>
        /// <param name="deltaTime">Delta time on origin</param>
        /// <param name="linearVelocity">Linear velocity of object on origin</param>
        /// <param name="isTeleport">Is this position a teleport ( far from previous position )</param>
        void RegisterPosition(Vector3 position, float deltaTime, Vector3 linearVelocity, bool isTeleport);

    }
}