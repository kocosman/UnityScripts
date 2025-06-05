using UnityEngine;
using OscJack;

public class OSCTransceiver : MonoBehaviour
{
    [Header("OSC Server (Outgoing)")]
    public string serverIP = "127.0.0.1";
    public int outgoingPort = 9000;

    [Header("OSC Receiver (Incoming)")]
    public int incomingPort = 8000;

    [Header("Send Settings")]
    public string oscAddress = "/message";
    public string messageToSend = "Hello, OSC!";

    private OscClient client;
    private OscServer server;

    void Start()
    {
        // Set up OSC client (for sending)
        client = new OscClient(serverIP, outgoingPort);

        // Set up OSC server (for receiving)
        server = new OscServer(incomingPort);
        server.MessageDispatcher.AddCallback("", OnAnyMessageReceived); // wildcard to receive all messages

        Debug.Log($"[OSCTransceiver] Sending to {serverIP}:{outgoingPort}, Receiving on port {incomingPort}");
    }

    private void OnAnyMessageReceived(string address, OscDataHandle data)
    {
        string receivedData = "";

        for (int i = 0; i < data.GetElementCount(); i++)
        {
            receivedData += data.GetElementAsString(i) + " ";
        }

        Debug.Log($"[OSC RECEIVED] Address: {address}, Data: {receivedData}");
    }

    public void SendMessageNow()
    {
        if (client != null)
        {
            client.Send(oscAddress, messageToSend);
            Debug.Log($"[OSC SENT] Address: {oscAddress}, Message: {messageToSend}");
        }
        else
        {
            Debug.LogWarning("[OSCTransceiver] OSC client not initialized.");
        }
    }

    void OnDestroy()
    {
        client?.Dispose();
        server?.Dispose();
    }
}
