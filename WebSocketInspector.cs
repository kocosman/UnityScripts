using UnityEngine;
using NativeWebSocket;
using System.Text;
using System.Threading.Tasks;

public class WebSocketInspector : MonoBehaviour
{
    [Header("WebSocket Settings")]
    public string serverAddress = "ws://127.0.0.1:3000"; // safer than localhost
    public string messageToSend = "Hello from Inspector!";
    
    [Header("Reconnection Settings")]
    public bool enableReconnect = true;
    public float reconnectInterval = 1.0f; // Time in seconds between reconnection attempts
    
    [Header("Debug Settings")]
    public bool showDebugLogs = true;
    
    private WebSocket websocket;
    private bool isConnecting = false;
    private bool shouldReconnect = false;
    private float reconnectTimer = 0f;
    
    // Reference to the mode parser
    private WebSocketModeParser modeParser;
    private WebSocketTakeoverTriggerParser takeoverTriggerParser;

    async void Start()
    {
        Application.targetFrameRate = 60;
        // Find or add the mode parser
        modeParser = GetComponent<WebSocketModeParser>();
        if (modeParser == null)
        {
            modeParser = gameObject.AddComponent<WebSocketModeParser>();
        }
        
        takeoverTriggerParser = GetComponent<WebSocketTakeoverTriggerParser>();
        if (takeoverTriggerParser == null)
        {
            takeoverTriggerParser = gameObject.AddComponent<WebSocketTakeoverTriggerParser>();
        }


        await ConnectToServer();
    }

    async Task ConnectToServer()
    {
        if (isConnecting) return;
        isConnecting = true;

        // Clean up existing connection if any
        if (websocket != null)
        {
            await websocket.Close();
        }

        // Create a new WebSocket
        websocket = new WebSocket(serverAddress);

        // Set up event handlers
        websocket.OnOpen += WebSocketOnOpen;
        websocket.OnError += WebSocketOnError;
        websocket.OnClose += WebSocketOnClose;
        websocket.OnMessage += WebSocketOnMessage;

        try
        {
            await websocket.Connect();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[WebSocket] Connection exception: " + ex.Message);
            isConnecting = false;
            if (enableReconnect)
            {
                shouldReconnect = true;
                if (showDebugLogs) Debug.Log("[WebSocket] Will attempt to reconnect...");
            }
        }
    }

    // Event handlers
    private void WebSocketOnOpen()
    {
        if (showDebugLogs) Debug.Log("[WebSocket] Connection opened.");
        isConnecting = false;
        shouldReconnect = false;
        reconnectTimer = 0f;
    }

    private void WebSocketOnError(string errorMsg)
    {
        Debug.LogError("[WebSocket] Error: " + errorMsg);
        isConnecting = false;
        if (enableReconnect)
        {
            shouldReconnect = true;
        }
    }

    private void WebSocketOnClose(WebSocketCloseCode closeCode)
    {
        if (showDebugLogs) Debug.Log("[WebSocket] Connection closed with code: " + closeCode);
        isConnecting = false;
        if (enableReconnect)
        {
            shouldReconnect = true;
            if (showDebugLogs) Debug.Log("[WebSocket] Will attempt to reconnect...");
        }
    }

    private void WebSocketOnMessage(byte[] data)
    {
        string message = Encoding.UTF8.GetString(data);
        if (showDebugLogs) Debug.Log("[WebSocket] Received message: " + message);
        
        // Pass the message to the mode parser
        modeParser.ParseMessage(message);
        takeoverTriggerParser.ParseMessage(message);
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null)
        {
            websocket.DispatchMessageQueue();
        }
#endif

        // Handle reconnection logic
        if (shouldReconnect && enableReconnect)
        {
            reconnectTimer += Time.deltaTime;
            
            if (reconnectTimer >= reconnectInterval)
            {
                reconnectTimer = 0f;
                if (showDebugLogs) Debug.Log("[WebSocket] Attempting to reconnect...");
                _ = ConnectToServer(); // Use discarded task to prevent warning
            }
        }
    }

    public bool IsConnected()
    {
        return websocket != null && websocket.State == WebSocketState.Open;
    }

    // Method used by the Inspector UI to send test messages
    public async void SendFromInspector()
    {
        if (IsConnected())
        {
            await websocket.SendText(messageToSend);
            if (showDebugLogs) Debug.Log("[WebSocket] Sent: " + messageToSend);
        }
        else
        {
            Debug.LogWarning("[WebSocket] WebSocket is not open. Message not sent.");
            
            // Optionally trigger a reconnection attempt
            if (enableReconnect && !shouldReconnect && !isConnecting)
            {
                shouldReconnect = true;
                if (showDebugLogs) Debug.Log("[WebSocket] Triggering reconnection attempt...");
            }
        }
    }

    // Method to manually disconnect
    public async void Disconnect()
    {
        shouldReconnect = false;
        if (websocket != null)
        {
            await websocket.Close();
        }
    }

    // Method to force a reconnection
    public async void ForceReconnect()
    {
        if (showDebugLogs) Debug.Log("[WebSocket] Forcing reconnection...");
        shouldReconnect = false; // Reset the auto-reconnect flag
        await ConnectToServer();
    }

    private async void OnApplicationQuit()
    {
        shouldReconnect = false;
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}