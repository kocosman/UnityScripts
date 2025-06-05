using UnityEngine;
using OscJack;
using System.Collections.Generic;

public class OSCServer : MonoBehaviour
{
    [Header("OSC Server Configuration")]
    [Tooltip("Port on which to receive OSC messages")]
    public int port = 8000;
    
    [Tooltip("Log all received OSC messages to the console")]
    public bool logAllMessages = true;
    
    [Header("Message Handling")]
    [Tooltip("List of specific OSC addresses to listen for")]
    public List<string> addressesToMonitor = new List<string>();
    
    // The actual OSC server instance
    private OscServer server;
    
    // Dictionary to store the last received value for each address
    private Dictionary<string, string> lastReceivedValues = new Dictionary<string, string>();
    
    void Start()
    {
        // Initialize the OSC server on the specified port
        server = new OscServer(port);
        
        // Add a callback for all messages if logging is enabled
        if (logAllMessages)
        {
            server.MessageDispatcher.AddCallback("", OnAnyMessageReceived);
        }
        
        // Add callbacks for specific addresses
        foreach (string address in addressesToMonitor)
        {
            server.MessageDispatcher.AddCallback(address, OnSpecificAddressReceived);
        }
        
        Debug.Log($"[OSC Server] Started listening on port {port}");
    }
    
    private void OnAnyMessageReceived(string address, OscDataHandle data)
    {
        string receivedData = FormatOscData(data);
        Debug.Log($"[OSC] Received on address: {address}, Data: {receivedData}");
    }
    
    private void OnSpecificAddressReceived(string address, OscDataHandle data)
    {
        string receivedData = FormatOscData(data);
        lastReceivedValues[address] = receivedData;
        
        // You can process specific messages here
        // For example, trigger events based on the address
    }
    
    // Helper method to format OSC data into a readable string
    private string FormatOscData(OscDataHandle data)
    {
        string result = "";
        int elementCount = data.GetElementCount();
        
        for (int i = 0; i < elementCount; i++)
        {
            if (i > 0) result += ", ";
            
            // Try to get the value as different types
            try 
            {
                float floatValue = data.GetElementAsFloat(i);
                result += floatValue.ToString("F2");
            }
            catch 
            {
                try 
                {
                    int intValue = data.GetElementAsInt(i);
                    result += intValue.ToString();
                }
                catch 
                {
                    // Default to string representation
                    result += data.GetElementAsString(i);
                }
            }
        }
        
        return result;
    }
    
    // Get the last received value for a specific address
    public string GetLastValue(string address)
    {
        if (lastReceivedValues.ContainsKey(address))
        {
            return lastReceivedValues[address];
        }
        return null;
    }
    
    // Method to add a new address to monitor at runtime
    public void AddAddressToMonitor(string address)
    {
        if (!addressesToMonitor.Contains(address))
        {
            addressesToMonitor.Add(address);
            server.MessageDispatcher.AddCallback(address, OnSpecificAddressReceived);
            Debug.Log($"[OSC Server] Now monitoring address: {address}");
        }
    }
    
    // Method to remove an address from monitoring at runtime
    public void RemoveAddressToMonitor(string address)
    {
        if (addressesToMonitor.Contains(address))
        {
            addressesToMonitor.Remove(address);
            server.MessageDispatcher.RemoveCallback(address, OnSpecificAddressReceived);
            Debug.Log($"[OSC Server] Stopped monitoring address: {address}");
        }
    }
    
    void OnDestroy()
    {
        // Clean up when the object is destroyed
        if (server != null)
        {
            server.Dispose();
            Debug.Log("[OSC Server] Stopped and disposed");
        }
    }
}