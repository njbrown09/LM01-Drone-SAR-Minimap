using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateDroneMessage : BaseMessage
{
    public string drone_name { get; set; }
    public float latitude { get; set; }
    public float longitude { get; set; }
    public float altitude { get; set; }
    public float velocity { get; set; }
    public float yaw { get; set; }
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
}
