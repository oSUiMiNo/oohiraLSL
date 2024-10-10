using NobleConnect.Mirror;
using UnityEngine;
using Mirror;

namespace NobleConnect.Examples.Mirror
{
    // A GUI for use with NobleNetworkManager
    public class ExampleMirrorNetworkHUD : MonoBehaviour
    {
        // The NetworkManager controlled by the HUD
        NobleNetworkManager networkManager;

        // The relay ip and port from the GUI text box
        string hostIP = "";
        string hostPort = "";

        // Used to determine which GUI to display
        bool isHost, isClient;

        // Get a reference to the NetworkManager
        public void Start()
        {
            // Cast from Unity's NetworkManager to a NobleNetworkManager.
            networkManager = (NobleNetworkManager)NetworkManager.singleton;
        }

        // Draw the GUI
        private void OnGUI()
        {
            if (!isHost && !isClient)
            {
                // Host button
                if (GUI.Button(new Rect(10, 10, 100, 30), "Host"))
                {
                    isHost = true;
                    isClient = false;

                    networkManager.StartHost();
                }

                // Client button
                if (GUI.Button(new Rect(10, 50, 100, 30), "Client"))
                {
                    networkManager.InitClient();
                    isHost = false;
                    isClient = true;
                }
            }
            else
            {
                // Host or client GUI
                if (isHost) GUIHost();
                else if (isClient) GUIClient();
            }
        }

        // Draw the host GUI
        void GUIHost()
        {
            if (networkManager.HostEndPoint == null)
            {
                // Display host status while initializing
                if (NobleServer.GetConnectedRegion() == GeographicRegion.AUTO)
                {
                    GUI.Label(new Rect(10, 10, 300, 22), "Selecting region..");
                }
                else
                {
                    GUI.Label(new Rect(10, 10, 300, 22), "Acquiring host address..");
                }
            }
            else
            {
                // Display host address, port, and region
                GUI.Label(new Rect(10, 10, 70, 22), "Host IP:");
                GUI.TextField(new Rect(80, 10, 420, 22), networkManager.HostEndPoint.Address.ToString(), "Label");
                GUI.Label(new Rect(10, 37, 70, 22), "Host Port:");
                GUI.TextField(new Rect(80, 37, 160, 22), networkManager.HostEndPoint.Port.ToString(), "Label");
                GUI.Label(new Rect(10, 64, 70, 22), "Region:");
                GUI.TextField(new Rect(80, 64, 300, 22), NobleServer.GetConnectedRegion().ToString(), "Label");
            }

            // Disconnect Button
            if (GUI.Button(new Rect(10, 108, 110, 30), "Disconnect"))
            {
                networkManager.StopHost();
                isHost = false;
            }

            if (!NobleServer.active) isHost = false;
        }

        // Draw the client GUI
        void GUIClient()
        {
            if (!networkManager.isNetworkActive)
            {
                // Text boxes for entering host's address
                GUI.Label(new Rect(10, 10, 150, 22), "Host IP:");
                hostIP = GUI.TextField(new Rect(170, 10, 420, 22), hostIP);
                GUI.Label(new Rect(10, 37, 150, 22), "Host Port:");
                hostPort = GUI.TextField(new Rect(170, 37, 160, 22), hostPort);

                // Connect button
                if (GUI.Button(new Rect(115, 81, 120, 30), "Connect"))
                {
                    networkManager.networkAddress = hostIP;
                    networkManager.networkPort = ushort.Parse(hostPort);
                    networkManager.StartClient();
                }

                // Back button
                if (GUI.Button(new Rect(10, 81, 95, 30), "Back"))
                {
                    isClient = false;
                }
            }
            else if (networkManager.client != null)
            {
                // Disconnect button
                GUI.Label(new Rect(10, 10, 300, 22), "Connection type: " + networkManager.client.latestConnectionType);
                GUI.Label(new Rect(10, 37, 300, 22), "Region: " + networkManager.client.GetConnectedRegion());
                if (GUI.Button(new Rect(10, 64, 110, 30), "Disconnect"))
                {
                    if (networkManager.client.isConnected)
                    {
                        // If we are already connected it is best to quit gracefully by sending
                        // a disconnect message to the host.
                        networkManager.client.Disconnect();
                    }
                    else
                    {
                        // If the connection is still in progress StopClient will cancel it
                        networkManager.StopClient();
                    }
                    isClient = false;
                }
            }
        }
    }
}