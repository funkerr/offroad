using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace com.onlineobject.objectnet {
#if UNITY_EDITOR
    /// <summary>
    /// This class is responsible for monitoring network gameplay by keeping track of network threads and sockets.
    /// It ensures that all network resources are properly released when exiting the play mode in the Unity Editor.
    /// </summary>
    [InitializeOnLoadAttribute]
    public class NetworkGamePlayMonitor {

        // A list to keep track of all the monitored network threads.
        private static List<NetworkThread> monitoredThreads = new List<NetworkThread>();

        // A list to keep track of all the monitored sockets.
        private static List<Socket> monitoredSockets = new List<Socket>();

        // Static constructor to subscribe to the play mode state change event.
        static NetworkGamePlayMonitor() {
            EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        /// <summary>
        /// This method is called whenever the play mode state changes in the Unity Editor.
        /// </summary>
        /// <param name="state">Play state</param>
        private static void LogPlayModeState(PlayModeStateChange state) {
            // When exiting play mode, clean up network resources.
            if (PlayModeStateChange.ExitingPlayMode.Equals(state)) {
                if (NetworkManager.Instance() != null) {
                    foreach (GameObject networkPrefab in NetworkManager.Instance().GetNetworkPrefabs()) {
                        // Intended to release network resources associated with the prefab.
                        // networkPrefab.ReleaseNetwork();
                    }
                    // Finish and terminate any pending threads.
                    while (NetworkGamePlayMonitor.monitoredThreads.Count > 0) {
                        NetworkThread thread = NetworkGamePlayMonitor.monitoredThreads[0];
                        NetworkGamePlayMonitor.monitoredThreads.RemoveAt(0);
                        try {
                            thread.Terminate();
                            NetworkDebugger.Log("Thread terminated");
                        } catch (Exception err) {
                            // Exception handling if thread termination fails.
                        }
                    }
                    // Close any open sockets.
                    while (NetworkGamePlayMonitor.monitoredSockets.Count > 0) {
                        Socket socket = NetworkGamePlayMonitor.monitoredSockets[0];
                        NetworkGamePlayMonitor.monitoredSockets.RemoveAt(0);
                        try {
                            if (socket.Connected) {
                                socket.Close();
                                NetworkDebugger.Log("Socket closed");
                            }
                        } catch (Exception err) {
                            // Exception handling if socket closure fails.
                        }
                    }
                }
            } else if (PlayModeStateChange.EnteredPlayMode.Equals(state)) {
                // Actions to take when entering play mode, if any.
            }
        }

        /// <summary>
        /// Registers a network thread to be monitored.
        /// </summary>
        /// <param name="thread">Thread to be monitored</param>
        public static void RegisterMonitoredThread(NetworkThread thread) {
            NetworkGamePlayMonitor.monitoredThreads.Add(thread);
        }

        /// <summary>
        /// // Registers a socket to be monitored.
        /// </summary>
        /// <param name="socket">Socket to be monitored</param>
        public static void RegisterMonitoredSocket(Socket socket) {
            NetworkGamePlayMonitor.monitoredSockets.Add(socket);
        }

    }

#endif
}