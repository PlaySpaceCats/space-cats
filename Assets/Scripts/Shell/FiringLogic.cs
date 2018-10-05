using UnityEngine;

public static class FiringLogic
{
    public static Vector3 CalculateFireVector(Shell shellToFire, Vector3 targetFirePosition, Vector3 firePosition,
        float launchAngle)
    {
        var target = targetFirePosition;
        target.y = firePosition.y;
        var toTarget = target - firePosition;
        var targetDistance = toTarget.magnitude;
        var shootingAngle = launchAngle;
        var grav = Mathf.Abs(Physics.gravity.y);
        grav *= shellToFire != null ? shellToFire.speedModifier : 1;
        var relativeY = firePosition.y - targetFirePosition.y;

        var theta = Mathf.Deg2Rad * shootingAngle;
        var cosTheta = Mathf.Cos(theta);
        var num = targetDistance * Mathf.Sqrt(grav) * Mathf.Sqrt(1 / cosTheta);
        var denom = Mathf.Sqrt(2 * targetDistance * Mathf.Sin(theta) + 2 * relativeY * cosTheta);
        var v = num / denom;

        var aimVector = toTarget / targetDistance;
        aimVector.y = 0;
        var rotAxis = Vector3.Cross(aimVector, Vector3.up);
        var rotation = Quaternion.AngleAxis(shootingAngle, rotAxis);
        aimVector = rotation * aimVector.normalized;

        return aimVector * v;
    }
}