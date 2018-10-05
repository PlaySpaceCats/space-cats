using UnityEngine;
using UnityEngine.EventSystems;

public class TankKeyboardInput : TankInputModule
{
    protected override bool DoFiringInput()
    {
        if (UIChat.IsActive())
        {
            SetFireIsHeld(false);
            return false;
        }
        if (EventSystem.current.IsPointerOverGameObject()) return false;

        if (!Input.mousePresent) return false;

        var mousePressed = Input.GetMouseButton(0);

        if (IsActiveModule || mousePressed)
        {
            var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            float hitDist;
            RaycastHit hit;
            if (Physics.Raycast(mouseRay, out hit, float.PositiveInfinity, GroundLayerMask))
                SetDesiredFirePosition(hit.point);
            else if (FloorPlane.Raycast(mouseRay, out hitDist)) SetDesiredFirePosition(mouseRay.GetPoint(hitDist));
        }

        SetFireIsHeld(mousePressed);

        return mousePressed;
    }

    protected override bool DoMovementInput()
    {
        if (UIChat.IsActive())
        {
            SetDesiredMovementDirection(Vector2.zero);
            return false;
        }
        var y = Input.GetAxisRaw("Vertical");
        var x = Input.GetAxisRaw("Horizontal");

        var cameraDirection = new Vector3(x, y, 0);

        if (cameraDirection.sqrMagnitude <= 0.01f)
        {
            SetDesiredMovementDirection(Vector2.zero);
            return false;
        }

        var worldUp = Camera.main.transform.TransformDirection(Vector3.up);
        worldUp.y = 0;
        worldUp.Normalize();
        var worldRight = Camera.main.transform.TransformDirection(Vector3.right);
        worldRight.y = 0;
        worldRight.Normalize();

        var worldDirection = worldUp * y + worldRight * x;
        var desiredDir = new Vector2(worldDirection.x, worldDirection.z);
        if (desiredDir.magnitude > 1) desiredDir.Normalize();
        SetDesiredMovementDirection(desiredDir);

        return true;
    }
}