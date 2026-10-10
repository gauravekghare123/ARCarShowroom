using UnityEngine;

/// Controls colour, doors, lights and turntable of the showroom car.
public class CarController : MonoBehaviour
{
    public Renderer[] bodyRenderers;
    public Renderer[] lampRenderers;
    public Light[] headlights;
    public Transform leftDoor, rightDoor;
    public float doorOpenAngle = 60f;
    public float doorSpeed = 90f;
    public float spinSpeed = 30f;

    bool doorsOpen, lightsOn, spinning;
    float doorAngle;

    void Start() { ApplyLights(); }

    void Update()
    {
        float target = doorsOpen ? doorOpenAngle : 0f;
        doorAngle = Mathf.MoveTowards(doorAngle, target, doorSpeed * Time.deltaTime);
        if (leftDoor)  leftDoor.localRotation  = Quaternion.Euler(0,  doorAngle, 0);
        if (rightDoor) rightDoor.localRotation = Quaternion.Euler(0, -doorAngle, 0);
        if (spinning) transform.Rotate(0, spinSpeed * Time.deltaTime, 0, Space.World);
    }

    public void SetColor(Color c)
    {
        foreach (var r in bodyRenderers)
        {
            if (!r) continue;
            var m = r.material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }

    public void ToggleDoors() { doorsOpen = !doorsOpen; }
    public void ToggleSpin()  { spinning = !spinning; }
    public void ToggleLights(){ lightsOn = !lightsOn; ApplyLights(); }

    void ApplyLights()
    {
        foreach (var l in headlights) if (l) l.enabled = lightsOn;
        foreach (var r in lampRenderers)
        {
            if (!r) continue;
            var m = r.material;
            if (lightsOn) m.EnableKeyword("_EMISSION"); else m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", lightsOn ? new Color(1f, 0.95f, 0.7f) * 3f : Color.black);
        }
    }
}
