using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Assets.Scripts.Drones;
using Assets.Scripts.Networking;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;



public class NetworkManager : MonoBehaviour
{
    
    //Initial Vars
    [Header("Settings")]
    public string ConnectionUrl;
    public GameObject DronePrefab;
    public BayesGridController BayesController;

    private Queue<BaseMessage> _IncomingMessages;
    public Dictionary<string, DroneBehaviour> Drones;
    
    WebSocket websocket;


    void Awake()
    {
        _IncomingMessages = new Queue<BaseMessage>();
        Drones = new Dictionary<string, DroneBehaviour>();
        Application.runInBackground = true;
    }

    // Start is called before the first frame update
    async void Start()
    {
        websocket = new WebSocket(ConnectionUrl);

        websocket.OnOpen += async () =>
        {
            Debug.Log("Connection open!");
            
            //Create auth message
            var authMessage = new AuthMessage
            {
                message_type = "authentication",
                client_type = "minimap"
            };
            
            //Send!
            await websocket.SendText(JsonConvert.SerializeObject(authMessage));
            
            Debug.Log("Sent packet!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += async (e) =>
        {
            Debug.Log("Connection closed! " + e);
            
            //Delete all drones
            foreach (var drone in Drones)
            {
                Destroy(drone.Value.gameObject);
            }
            
            //Clear tje drpmes
            Drones.Clear();
            
            //Clear the queue
            lock(_IncomingMessages)
                _IncomingMessages.Clear();
            
            //Reconnect
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
            #endif
            
            websocket = new WebSocket(ConnectionUrl);
            // waiting for messages
            Start();
        };

        websocket.OnMessage += (bytes) =>
        {
            
            //Deserialize the string from the bytes
            var rawMessage = Encoding.UTF8.GetString(bytes);
            
            //Deserialize the message
            var baseMessage = JsonConvert.DeserializeObject<BaseMessage>(rawMessage);
            
            //Check the type
            switch (baseMessage.message_type)
            {
                //Create drone message
                case "CreateDrone":
                    
                    //Deserialize
                    var createDroneMessage = JsonConvert.DeserializeObject<CreateDroneMessage>(rawMessage);
                    
                    //Add to queue
                    lock (_IncomingMessages)
                    {
                        _IncomingMessages.Enqueue(createDroneMessage);
                    }
                    
                    break;
                
                //Update drone message
                case "UpdateDrone":
                    //Deserialize
                    var updateDroneMessage = JsonConvert.DeserializeObject<UpdateDroneMessage>(rawMessage);
                    
                    //Add to queue
                    lock (_IncomingMessages)
                    {
                        _IncomingMessages.Enqueue(updateDroneMessage);
                    }
                    
                    break;
                
                //Update drone cluster message
                case "UpdateDroneCluster":
                    //Deserialize
                    var updateDroneClusterMessage = JsonConvert.DeserializeObject<UpdateDroneClusterMessage>(rawMessage);
                    
                    //Add to queue
                    lock (_IncomingMessages)
                    {
                        _IncomingMessages.Enqueue(updateDroneClusterMessage);
                    }
                    
                    break;
                
                //Update drone cluster message
                case "UpdateGrid":
                    
                    Debug.Log(rawMessage);
                    //Deserialize
                    var updateGridMessage = JsonConvert.DeserializeObject<UpdateGridMessage>(rawMessage);
                    
                    //Add to queue
                    lock (_IncomingMessages)
                    {
                        _IncomingMessages.Enqueue(updateGridMessage);
                    }
                    
                    break;
                    
            }
        };

        // Keep sending messages at every 0.3s
        InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);

        Thread.Sleep(1000);
        // waiting for messages
        await websocket.Connect();
    }

    void Update()
    {
        //SendWebSocketMessage();
        
        //Lock the queue
        lock (_IncomingMessages)
        {
            
            //Print the messages
            while (_IncomingMessages.Count > 0)
            {
                //Get the message
                var newMessage = _IncomingMessages.Dequeue();
                
                //Process the message
                if (newMessage is CreateDroneMessage)
                {
                    //Convert to drone create object
                    var createMessage = (CreateDroneMessage) newMessage;
                    
                    //Get the drone position
                    var dronePosition = new Vector3(createMessage.y, createMessage.x, 0);
                    
                    //Create a new drone
                    var newDroneObject = Instantiate(DronePrefab, dronePosition, Quaternion.identity);
                    
                    //Get the behaviour
                    var newDroneBehaviour = newDroneObject.GetComponent<DroneBehaviour>();
                    
                    //Set the position
                    newDroneObject.transform.position = dronePosition;
                    newDroneBehaviour.UpdateTransform(createMessage.x,createMessage.y,createMessage.z, 0);
                    
                    //Reset the trail
                    newDroneBehaviour.ResetTrail();
                    
                    //Set the name
                    newDroneBehaviour.Name = createMessage.drone_name;
                    
                    //Add to dictionary
                    Drones.Add(createMessage.drone_name, newDroneBehaviour);
                    
                    //Logging
                    Debug.Log(("Created Drone: " + createMessage.drone_name));
                }
                
                //Process the message
                if (newMessage is UpdateDroneMessage)
                {
                    //Convert to drone create object
                    var updateMessage = (UpdateDroneMessage) newMessage;
                    
                    //Get the drone
                    var droneToUpdate = Drones[updateMessage.drone_name];
                    
                    //Update the drone state
                    droneToUpdate.UpdateState(updateMessage.latitude, updateMessage.longitude, updateMessage.altitude, updateMessage.velocity);
                    
                    //Update the drone transform
                    droneToUpdate.UpdateTransform(updateMessage.x, updateMessage.y, updateMessage.z, updateMessage.yaw);
                }
                
                //Process the message
                if (newMessage is UpdateDroneClusterMessage)
                {
                    //Convert to drone create object
                    var updateMessage = (UpdateDroneClusterMessage) newMessage;
                    
                    //Get the cluster drones
                    var clusterDrone = Drones[updateMessage.cluster_name];
                    
                    // Iterate over the list of drone names in the update message
                    foreach (var droneName in updateMessage.drone_names)
                    {
                        // Check if the current drone name is in the Drones dictionary
                        if (Drones.TryGetValue(droneName, out var drone))
                        {
                           //Set the cluster
                           drone.SetDroneType(EDroneType.Wolf);
                           drone.UpdateClusterParent(clusterDrone);
                        }
                    }
                    
                    //Set the drone type of the observer
                    var observerDrone = Drones[updateMessage.cluster_name];
                    
                    observerDrone.SetDroneType(EDroneType.Overseer);

                }
                
                //Process the message
                if (newMessage is UpdateGridMessage)
                {
                    //Convert to drone create object
                    var updateMessage = (UpdateGridMessage) newMessage;
                    
                    Debug.Log("Updating grid:");
                    Debug.Log(updateMessage.data);
                    BayesController.UpdateBayes(updateMessage.data);
                    
                }
            }
            
        }
        
        
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
        


    }

    async void SendWebSocketMessage()
    {
        if (websocket.State == WebSocketState.Open)
        {
            // Sending bytes
            await websocket.Send(new byte[] {  });
        }
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }

}
