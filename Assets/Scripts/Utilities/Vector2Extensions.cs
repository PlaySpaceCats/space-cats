using UnityEngine;

public static class Vector2Extensions
{
    public static Vector2 Rotate(this Vector2 v, float degrees)
    {
        var rads = degrees * Mathf.Deg2Rad;

        var cos = Mathf.Cos(rads);
        var sin = Mathf.Sin(rads);

        var vx = v.x;
        var vy = v.y;

        return new Vector2(cos * vx - sin * vy, sin * vx + cos * vy);
    }
}