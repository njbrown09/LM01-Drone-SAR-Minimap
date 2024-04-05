using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Settings
    public bool AutoTracking { get; set; }
    
    public NetworkManager networkManager; // Reference to the NetworkManager
    public float padding = 2f; // Padding to add around the furthest drones
    public float zoomSpeed = 10f; // Speed of zooming in and out
    public float panSpeed = 20f; // Speed of panning

    private Camera cam;
    private Vector3 dragOrigin; // To keep track of the point where the mouse drag started

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>(); // Get the Camera component
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager reference not set in CameraController");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (AutoTracking)
        {
            if (networkManager != null && networkManager.Drones.Count > 0)
            {
                AdjustCameraView();
            }
        }
        else
        {
            HandleZoom();
            HandlePan();
        }
    }

    void AdjustCameraView()
    {
        if (networkManager.Drones.Count == 0) return;

        var dronePositions = new List<Vector2>();
        foreach (var droneEntry in networkManager.Drones)
        {
            dronePositions.Add(droneEntry.Value.transform.position);
        }

        // Calculate the bounding box that contains all drones
        var bounds = new Bounds(dronePositions[0], Vector3.zero);
        foreach (var pos in dronePositions)
        {
            bounds.Encapsulate(pos);
        }

        float screenAspect = Screen.width / (float)Screen.height;
        float boundAspect = bounds.size.x / bounds.size.y;

        // Calculate camera size based on the aspect ratio of the bounds relative to the screen
        float cameraSize;
        if (screenAspect >= boundAspect)
        {
            cameraSize = bounds.size.y / 2;
        }
        else
        {
            cameraSize = bounds.size.x / (2 * screenAspect);
        }

        //Clamp camera size
        cameraSize = Mathf.Max(40f, cameraSize);
        
        // Calculate what 10% of the screen's height is in world units
        float tenPercentScreenHeight = 0.1f * cam.orthographicSize;

        // Set the camera size and adjust for the 10% at the top of the screen
        // This will effectively lower the viewport so the top banner doesn't obscure the view
        if (AutoTracking)
            cam.orthographicSize = Mathf.Max(cameraSize + padding, 1f) + tenPercentScreenHeight;

        // Center the camera on the bounding box's center
        // Since we've increased the camera size, we no longer need to move the camera position itself down
        if (AutoTracking)
        {
            Vector3 newCameraPosition = new Vector3(bounds.center.x, bounds.center.y + (tenPercentScreenHeight),
                cam.transform.position.z);
            cam.transform.position = newCameraPosition;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        cam.orthographicSize -= scroll * zoomSpeed;
        cam.orthographicSize = Mathf.Max(cam.orthographicSize, 5f); // Prevent the camera from zooming too close
    }

    void HandlePan()
    {
        if (Input.GetMouseButtonDown(1)) // Right mouse button clicked
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(1)) // Right mouse button held down
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position += difference;
        }
    }
}
