using UnityEngine;

/// Methods wired to the UI buttons.
public class ShowroomUI : MonoBehaviour
{
    public ARCarPlacer placer;
    public Color[] colors = { new Color(0.8f,0.05f,0.05f), new Color(0.1f,0.3f,0.9f), Color.white, new Color(0.05f,0.05f,0.05f) };

    public void SetColorIndex(int i) { if (placer.Car && i >= 0 && i < colors.Length) placer.Car.SetColor(colors[i]); }
    public void ToggleDoors()  { if (placer.Car) placer.Car.ToggleDoors(); }
    public void ToggleLights() { if (placer.Car) placer.Car.ToggleLights(); }
    public void ToggleSpin()   { if (placer.Car) placer.Car.ToggleSpin(); }
    public void Reposition()   { placer.StartReposition(); }
    public void ResetCar()     { placer.ClearCar(); }
}
