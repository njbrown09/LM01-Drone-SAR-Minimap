using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Drones;
using TMPro;
using UnityEngine;

public class DroneBehaviour : MonoBehaviour
{

    [Header("References")] public TMP_Text NameText;
    public TMP_Text AltitudeText;
    public TrailRenderer TrailRenderer;

    [Header("Visual References")] 
    public GameObject RedIcon;
    public GameObject BlueIcon;
    public GameObject OrangeIcon;
    public LineRenderer StringLineRenderer;

    [Header("Drone State")] 
    public string Name;
    public float Latitude;
    public float Longitude;
    public float Altitude;
    public float Velocity;
    public float Yaw;
    private float _TimeSinceUpdate;
    
    //Drone Transform
    private Vector3 _PreviousPosition;
    private Vector3 _Position;
    private DroneBehaviour _ClusterParentDrone;
    private EDroneType _DroneType;
    
    // Update is called once per frame
    void Update()
    {
        
        //Get the current pos
        var currPos = transform.position;
        
        // Add the time since the last frame
        _TimeSinceUpdate += Time.deltaTime;
    
        // Calculate the lerp factor. Assuming you have a desired duration to complete the lerp, e.g., 1 second
        float lerpDuration = 1/20f; // You can adjust this duration based on the expected movement speed
        float lerpFactor = _TimeSinceUpdate / lerpDuration;
        lerpFactor = Mathf.Clamp(lerpFactor, 0, 1); // Ensure lerpFactor stays between 0 and 1

        // Smoothly interpolate the position from _PreviousPosition to _Position
        transform.position = Vector3.Lerp(_PreviousPosition, _Position, lerpFactor);

        
        //Reset the trail
        if (Vector3.Distance(currPos, _Position) > 10f)
            ResetTrail();
        
        //Update the linerenderer
        if (_DroneType == EDroneType.Wolf)
        {
            StringLineRenderer.SetPosition(0, transform.position);
            StringLineRenderer.SetPosition(1, _ClusterParentDrone.transform.position);
        }

        //Set text
        NameText.SetText(Name);
        AltitudeText.SetText( + (int)-Altitude + "m");
    }

    public void UpdateState(float latitude, float longitude, float altitude, float velocity)
    {
        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
        Velocity = velocity;
    }

    public void ResetTrail()
    {
        TrailRenderer.Clear();
    }
    
    public void UpdateTransform(float x, float y, float z, float yaw)
    {

        Yaw = yaw;
        
        //Set the position
        _PreviousPosition = _Position;
        _Position = new Vector3(y, x, 0);
        
        _TimeSinceUpdate = 0; 
    }

    public void SetDroneType(EDroneType type)
    {
        _DroneType = type;
        
        //CHeck the drone type
        RedIcon.SetActive(type == EDroneType.Target);
        OrangeIcon.SetActive(type == EDroneType.Overseer);
        BlueIcon.SetActive(type == EDroneType.Wolf);
        
        
        
        //Enable the line renderer
        StringLineRenderer.enabled = type == EDroneType.Wolf;
    }

    public void UpdateClusterParent(DroneBehaviour clusterParent)
    {
        _ClusterParentDrone = clusterParent;
    }
}
