using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateDroneMessage : BaseMessage
{
    public string drone_name { get; set; }
    public float x { get; set; }
    
    public float y { get; set; }
    
    public float z { get; set; }
}
