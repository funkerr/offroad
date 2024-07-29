using UnityEngine;

namespace com.onlineobject.objectnet.examples {
    public class PredictionProvider : MonoBehaviour, IPrediction {

        /// <summary>
        /// Predict position of object
        /// </summary>
        /// <param name="nextPosition">Next position on buffered positions to interpolate</param>
        /// <param name="linearVelocity">Linear velocity of object on origin</param>
        /// <param name="deltaTime">Delta time on origin</param>
        /// <param name="currentFPS">Current FPS where login is being executed</param>
        /// <returns></returns>
        public Vector3 Predict(Vector3 nextPosition, Vector3 linearVelocity, float deltaTime, int currentFPS) {
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Here you will need to calculate the possible position where position was original sent
            // you can use all attributes provided on method to assume where object would be based on latency, speed, velocity, etc...
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            return nextPosition; // I'm going to return provided position
        }

        /// <summary>
        /// Register received position
        /// </summary>
        /// <param name="position">Received position</param>
        /// <param name="deltaTime">Delta time on origin</param>
        /// <param name="linearVelocity">Linear velocity of object on origin</param>
        /// <param name="isTeleport">Is this position a teleport ( far from previous position )</param>
        public void RegisterPosition(Vector3 position, float deltaTime, Vector3 linearVelocity, bool isTeleport) {
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Here you can store those data to future use when you need to predict position
            /// -------------------------------------------------------------------------------------------------------------------------
            /// Note: The values provided by "Predict" is alredy cached on came from internal prediction system, you only need to cache 
            /// by yourself for some custom behavior
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }
    }
}