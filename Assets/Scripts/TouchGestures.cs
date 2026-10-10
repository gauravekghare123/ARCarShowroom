using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// One-finger drag = rotate, two-finger pinch = scale. Editor: right-drag rotate, scroll scale.
public class TouchGestures : MonoBehaviour
{
    public ARCarPlacer placer;
    public float rotateSpeed = 0.4f, minScale = 0.05f, maxScale = 2f;

    void Update()
    {
        if (placer == null || placer.Car == null) return;
        Transform t = placer.Car.transform;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            TouchControl first = null;
            TouchControl second = null;
            bool overUI = false;

            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.press.isPressed) continue;

                int touchId = touch.touchId.ReadValue();
                if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(touchId))
                {
                    overUI = true;
                    continue;
                }

                if (first == null) first = touch;
                else if (second == null) second = touch;
            }

            if (overUI) return;

            if (first != null && second != null)
            {
                Vector2 firstPosition = first.position.ReadValue();
                Vector2 secondPosition = second.position.ReadValue();
                float previousDistance = ((firstPosition - first.delta.ReadValue()) -
                                          (secondPosition - second.delta.ReadValue())).magnitude;
                float currentDistance = (firstPosition - secondPosition).magnitude;
                if (previousDistance > 0.01f) Scale(t, currentDistance / previousDistance);
                return;
            }

            if (first != null)
            {
                if (first.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved)
                    t.Rotate(0, -first.delta.ReadValue().x * rotateSpeed, 0, Space.World);
                return;
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            bool overUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject(-1);
            if (overUI) return;

            float s = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(s) > 0.01f) Scale(t, 1f + s * 0.05f);
            if (mouse.rightButton.isPressed)
                t.Rotate(0, -mouse.delta.ReadValue().x * 5f, 0, Space.World);
        }
    }

    void Scale(Transform t, float factor)
    {
        float n = Mathf.Clamp(t.localScale.x * factor, minScale, maxScale);
        t.localScale = Vector3.one * n;
    }
}
