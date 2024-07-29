#if STEAMWORKS_NET
using Steamworks;
using System.Linq;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.onlineobject.objectnet {
    public class NetworkSteamManager : MonoBehaviour {
        // Flag is this object shall keep persistent between scenes
        [SerializeField]
        private bool dontDestroyOnLoad = true;

#if STEAMWORKS_NET
        [SerializeField]
        private ELobbyType lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;

        [SerializeField]
        private ELobbyDistanceFilter lobbyDistance = ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide;
#endif

        [SerializeField]
        private bool autoRefresh = false;

        [SerializeField]
        private int refreshRate = 1000;

        [SerializeField]
        private int maximumOfPlayers = 4;

        private string creationLobbyName;

        private ulong currentLobbyID;

        private SteamLobby currentLobby;

        private Action<bool> onPlayerJoinedOnLobby;

        // List containing all current lobbies
        private List<SteamLobby> currentLobbies = new List<SteamLobby>();

        private float nextRefresh = 0f;

        private Action filterProcedure;

        private Dictionary<string, string> metadata = new Dictionary<string, string>();

#if STEAMWORKS_NET
        protected Callback<GameLobbyJoinRequested_t>    JoinRequest;
        protected Callback<LobbyCreated_t>              LobbyCreated;
        protected Callback<LobbyEnter_t>                LobbyEntered;
        protected Callback<LobbyMatchList_t>            LobbyListRefresh;        
#endif

        private static NetworkSteamManager instance;

        /// <summary>
        /// Provides access to the singleton instance of the NetworkManager.
        /// </summary>
        /// <returns>The singleton instance of the NetworkManager.</returns>
        public static NetworkSteamManager Instance() {
            // Check if an instance already exists
#if DEBUG
            if (!InstanceExists()) {
                // Warn the user if the application is running and no instance is found
                if (Application.isPlaying) {
                    NetworkDebugger.LogWarning("[ NetworkSteam ] Could not find the instance of object. Please ensure you have added the NetworkSteam Prefab to your scene.");
                }
            }
#endif
            // Return the singleton instance
            return NetworkSteamManager.instance;
        }

        /// <summary>
        /// Detect ig another NetworkSteam is already instantiated
        /// </summary>
        /// <returns>True if NetworkSteam already exists, otherwise false</returns>
        public bool DetectDuplicated() {
            return (NetworkSteamManager.instance != null);
        }

        /// <summary>
        /// Flag current instance of NetworkSteam as the in use instance
        /// </summary>
        private void SetInstance() {
            NetworkSteamManager.instance = this;
        }

        /// <summary>
        /// Checks if an instance of NetworkSteam already exists.
        /// </summary>
        /// <returns>True if an instance exists, false otherwise.</returns>
        private static bool InstanceExists() {
            return (NetworkSteamManager.instance != null); // Check if the instance is not null
        }

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        void Awake() {
            if ((this.dontDestroyOnLoad) &&
                (this.DetectDuplicated())) {
                DestroyImmediate(this.gameObject);
            } else {
                // Flag on base class
                if (this.dontDestroyOnLoad) {
                    DontDestroyOnLoad(this);
                }
                this.SetInstance(); // Flag instance to be the current object
            }
        }

        private void Start() {
#if STEAMWORKS_NET
            this.LobbyCreated       = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            this.JoinRequest        = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
            this.LobbyEntered       = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            this.LobbyListRefresh   = Callback<LobbyMatchList_t>.Create(OnLobbyRefreshResult);
#endif
        }

        private void LateUpdate() {
            if (this.autoRefresh) {
                if (this.nextRefresh < Time.time) {
                    this.nextRefresh = (Time.time + this.refreshRate);
                    this.RequestLobbyList(this.filterProcedure);
                }
            }
        }

        /// <summary>
        /// Create a new lobby instance
        /// </summary>
        /// <param name="lobbyName">The name of the new lobby</param>
        /// <param name="extraTags">Extra parameter added to this lobby (key/value pair)</param>
        /// <note type="tip">
        /// Use extra tags to flag any informations that you wish to be filtered on your game
        /// </note>
        public void CreateLobby(string lobbyName = "", params (string, string)[] extraTags) {
            this.creationLobbyName = lobbyName;
            this.metadata.Clear();
            foreach((string, string) data in extraTags) {
                this.metadata.Add(data.Item1, data.Item2);
            }
#if STEAMWORKS_NET
            SteamMatchmaking.CreateLobby(this.lobbyType, this.maximumOfPlayers);
#endif
        }

#if STEAMWORKS_NET
        public SteamLobby[] GetLobbies() {
            return this.currentLobbies.ToArray<SteamLobby>();
        }
#endif

        public void RequestLobbyList(Action filter = null) {
#if STEAMWORKS_NET
            this.filterProcedure = filter;
            if (this.filterProcedure != null) {
                this.filterProcedure.Invoke();
            } else {
                SteamMatchmaking.AddRequestLobbyListDistanceFilter(this.lobbyDistance);
            }
            SteamMatchmaking.RequestLobbyList();
#endif
        }

#if STEAMWORKS_NET
        private void OnLobbyCreated(LobbyCreated_t callback) {
            if (callback.m_eResult != EResult.k_EResultOK)
                return;
            currentLobbyID = callback.m_ulSteamIDLobby;

            SteamLobby lobbyData = new CSteamID(currentLobbyID);
            lobbyData.IsSession = true;
            lobbyData[SteamLobby.DataName] = this.creationLobbyName;
            SteamMatchmaking.SetLobbyData(lobbyData, "HostAddress",  SteamUser.GetSteamID().ToString());
            SteamMatchmaking.SetLobbyData(lobbyData, "LobbyName",    this.creationLobbyName);
            // Add custom metadata
            foreach(var data in this.metadata) {
                SteamMatchmaking.SetLobbyData(lobbyData, data.Key, data.Value);
            }
            this.currentLobby = lobbyData;

            NetworkManager.Instance().ConfigureMode(NetworkConnectionType.Server);
            NetworkManager.Instance().SetServerAddress(SteamUser.GetSteamID().ToString());

            NetworkDebugger.Log("Server Created Lobby: {0}", currentLobbyID);
        }
#endif

#if STEAMWORKS_NET
        private void OnJoinRequest(GameLobbyJoinRequested_t callback) {
            SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }
#endif

#if STEAMWORKS_NET
        private void OnLobbyEntered(LobbyEnter_t callback) {
            currentLobbyID = callback.m_ulSteamIDLobby;
            if (NetworkManager.Instance().IsServerConnection()) {
                NetworkManager.Instance().StartNetwork();
                NetworkDebugger.Log("Server lobby started");
            } else {
                NetworkDebugger.Log("Client joined on lobby");
                try {
                    if (this.onPlayerJoinedOnLobby != null) {
                        this.onPlayerJoinedOnLobby.Invoke(true);
                    }
                } finally {
                    NetworkManager.Instance().ConfigureMode(NetworkConnectionType.Client);
                    NetworkManager.Instance().SetServerAddress(SteamMatchmaking.GetLobbyData(new CSteamID(currentLobbyID), "HostAddress"));
                    NetworkManager.Instance().StartNetwork();
                }                
            }

        }
#endif

#if STEAMWORKS_NET
        private void OnLobbyRefreshResult(LobbyMatchList_t lobbyListResult) {
            this.currentLobbies.Clear();
            for (int i = 0; i < lobbyListResult.m_nLobbiesMatching; i++) {
                this.currentLobbies.Add( SteamMatchmaking.GetLobbyByIndex(i) );      
                if (this.currentLobby.SteamId == this.currentLobbies[this.currentLobbies.Count - 1].SteamId) {
                    NetworkDebugger.Log("Lobby on list: " + this.currentLobby.SteamId);
                }
            }
        }
#endif

#if STEAMWORKS_NET
        public void RequestToJoin(CSteamID steamID, Action<bool> onLobbyJoined = null) {
            NetworkDebugger.Log("Attempting to join lobby with ID: {0}", steamID.m_SteamID.ToString());
            this.onPlayerJoinedOnLobby = onLobbyJoined;
            if (SteamMatchmaking.RequestLobbyData(steamID))
                SteamMatchmaking.JoinLobby(steamID);
            else
                NetworkDebugger.Log("Failed to join lobby with ID: {0}", steamID.m_SteamID.ToString());
        }
#endif

#if STEAMWORKS_NET
        public void LeaveLobby() {
            SteamMatchmaking.LeaveLobby(new CSteamID(currentLobbyID));
            currentLobbyID = 0;
            // Close network connection
            NetworkManager.Instance().StopNetwork();
        }
#endif
    }
}