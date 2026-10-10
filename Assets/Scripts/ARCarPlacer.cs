using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// Tap on a detected floor plane to place the car.
/// If no ARRaycastManager exists (Editor testing), the car appears 3 m in front of the camera.
public class ARCarPlacer : MonoBehaviour
{
    public GameObject carPrefab;
    public float placementScale = 0.25f;   // 4 m car -> 1 m in the room

    public CarController Car { get; private set; }
    public bool RepositionMode { get; private set; }

    ARRaycastManager rays;
    ARPlaneManager planes;
    Text instructionText;
    string lastInstruction;
    float nextInstructionUpdate;
    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        rays = FindFirstObjectByType<ARRaycastManager>();
        planes = FindFirstObjectByType<ARPlaneManager>();

        foreach (Text label in FindObjectsByType<Text>(FindObjectsSortMode.None))
        {
            if (label.text.StartsWith("Scan the floor"))
            {
                instructionText = label;
                break;
            }
        }

        RefreshInstruction();
    }

    void Update()
    {
        if (Time.unscaledTime >= nextInstructionUpdate)
        {
            nextInstructionUpdate = Time.unscaledTime + 0.25f;
            RefreshInstruction();
        }

        if (!TryGetTap(out Vector2 pos)) return;
        if (Car != null && !RepositionMode) return;
        if (!TryGetPose(pos, out Vector3 point)) return;

        if (Car == null)
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            Vector3 f = camera.transform.forward; f.y = 0;
            Quaternion rot = f.sqrMagnitude > 0.001f ? Quaternion.LookRotation(-f) : Quaternion.identity;
            var go = Instantiate(carPrefab, point, rot);
            go.transform.localScale = Vector3.one * placementScale;
            Car = go.GetComponent<CarController>();
        }
        else
        {
            Car.transform.position = point;
            RepositionMode = false;
        }
    }

    public void StartReposition() { if (Car != null) RepositionMode = true; }
    public void ClearCar() { if (Car != null) Destroy(Car.gameObject); Car = null; RepositionMode = false; }

    void RefreshInstruction()
    {
        if (instructionText == null) return;

        string message;
        if (ARSession.state == ARSessionState.Unsupported)
        {
            message = "AR is unavailable here. Run this app on an ARCore-supported phone.";
        }
        else if (ARSession.state == ARSessionState.NeedsInstall || ARSession.state == ARSessionState.Installing)
        {
            message = "Installing or updating AR services…";
        }
        else if (ARSession.state != ARSessionState.SessionTracking)
        {
            message = "Starting AR camera… Move your phone slowly over a flat surface.";
        }
        else if (RepositionMode)
        {
            message = "Tap a detected surface to move the car.";
        }
        else if (Car != null)
        {
            message = "Car placed. Drag to rotate; pinch to scale.";
        }
        else if (planes != null && planes.trackables.count > 0)
        {
            message = "Surface detected. Tap the surface to place the car.";
        }
        else
        {
            message = "Camera ready. Move your phone slowly to scan a flat surface.";
        }

        string text = message + "\nDrag = rotate   Pinch = scale";
        if (text == lastInstruction) return;

        lastInstruction = text;
        instructionText.text = text;
    }

    bool TryGetTap(out Vector2 pos)
    {
        pos = default;
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var touch = touchscreen.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                int touchId = touch.touchId.ReadValue();
                if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(touchId)) return false;
                pos = touch.position.ReadValue();
                return true;
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(-1)) return false;
            pos = mouse.position.ReadValue();
            return true;
        }
        return false;
    }

    bool TryGetPose(Vector2 screenPos, out Vector3 point)
    {
        point = default;
        if (rays != null)
        {
            if (rays.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            { point = hits[0].pose.position; return true; }
            return false;
        }
        // Editor fallback (no AR)
        Camera camera = Camera.main;
        if (camera == null) return false;

        Vector3 f = camera.transform.forward; f.y = 0; f.Normalize();
        point = new Vector3(camera.transform.position.x, 0, camera.transform.position.z) + f * 3f;
        return true;
    }
}
