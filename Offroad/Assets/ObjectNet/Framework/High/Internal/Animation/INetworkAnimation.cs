namespace com.onlineobject.objectnet {
    /// <summary>
    /// Define an interface for network animation control.
    /// </summary>
    public interface INetworkAnimation {

        /// <summary>
        /// Initializes the network animation system.
        /// This method should set up any necessary state or resources needed before animations can be played.
        /// </summary>
        void Intialize();

        /// <summary>
        /// Plays an animation state on a specific layer.
        /// </summary>
        /// <param name="stateName">The name of the animation state to play.</param>
        /// <param name="layerIndex">The index of the layer where the animation will be played.</param>
        void Play(string stateName, int layerIndex);

        /// <summary>
        /// Plays an animation state on the default layer.
        /// </summary>
        /// <param name="stateName">The name of the animation state to play.</param>
        void Play(string stateName);

    }

}