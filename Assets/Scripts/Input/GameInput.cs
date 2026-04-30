using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public static class GameInput
{
    public static bool IsBackPressedThisFrame()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    public static int GetPressedTouchCount()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
        {
            return 0;
        }

        int pressedTouchCount = 0;

        foreach (TouchControl touch in touchscreen.touches)
        {
            if (touch.press.isPressed)
            {
                pressedTouchCount++;
            }
        }

        return pressedTouchCount;
    }

    public static bool TryGetPointerDownPosition(out Vector2 screenPosition)
    {
        if (TryGetTouchPosition(touch => touch.press.wasPressedThisFrame, out screenPosition))
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    public static bool TryGetPointerHeldPosition(out Vector2 screenPosition)
    {
        if (TryGetTouchPosition(touch => touch.press.isPressed, out screenPosition))
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    public static bool TryGetPointerUpPosition(out Vector2 screenPosition)
    {
        if (TryGetTouchPosition(touch => touch.press.wasReleasedThisFrame, out screenPosition))
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    public static bool TryGetCurrentPointerScreenPosition(out Vector2 screenPosition)
    {
        if (TryGetTouchPosition(touch => touch.press.isPressed, out screenPosition))
        {
            return true;
        }

        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    private static bool TryGetTouchPosition(System.Func<TouchControl, bool> predicate, out Vector2 screenPosition)
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (predicate(touch))
                {
                    screenPosition = touch.position.ReadValue();
                    return true;
                }
            }
        }

        screenPosition = default;
        return false;
    }
}
