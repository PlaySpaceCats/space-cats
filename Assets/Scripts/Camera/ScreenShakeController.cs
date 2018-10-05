using System;
using System.Collections.Generic;
using Tanks.Utilities;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ScreenShakeController : Singleton<ScreenShakeController>
{
    private List<ShakeInstance> m_CurrentShakes;

    [SerializeField] protected float m_DirectionNoiseScale;

    [SerializeField] protected float m_MagnitudeNoiseScale;

    [SerializeField] protected ShakeSettings m_OrthographicSettings;

    [SerializeField] protected ShakeSettings m_PerspectiveSettings;

    private int m_ShakeCounter;

    private Camera m_ShakingCamera;

    protected override void Awake()
    {
        base.Awake();

        m_CurrentShakes = new List<ShakeInstance>();

        m_ShakingCamera = GetComponent<Camera>();
        if (m_ShakingCamera != null)
        {
            return;
        }

        enabled = false;
        Debug.LogWarning("No camera for ScreenShakeController.");
    }

    protected virtual void Update()
    {
        if (m_ShakingCamera == null)
            return;

        var shakeIntensity = Vector2.zero;

        for (var i = m_CurrentShakes.Count - 1; i >= 0; --i)
        {
            var shake = m_CurrentShakes[i];

            ProcessShake(ref shake, ref shakeIntensity);

            if (shake.done)
                m_CurrentShakes.RemoveAt(i);
            else
                m_CurrentShakes[i] = shake;
        }

        var shake3D = new Vector3(shakeIntensity.x, shakeIntensity.y, 0);

        if (m_ShakingCamera.orthographic)
        {
            transform.localPosition = shake3D;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            var rotateAxis = Vector3.Cross(Vector3.forward, shake3D).normalized;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.AngleAxis(shake3D.magnitude, rotateAxis);
        }
    }

    public void DoShake(Vector3 worldPosition, float magnitude, float duration)
    {
        var viewportPos = m_ShakingCamera.WorldToViewportPoint(worldPosition);
        var relativePos = new Vector2(viewportPos.x * 2 - 1, viewportPos.y * 2 - 1);

        DoShake(relativePos.normalized, magnitude, duration);
    }

    public void DoShake(Vector3 worldPosition, float magnitude, float duration,
        float minScale, float maxScale)
    {
        var viewportPos = m_ShakingCamera.WorldToViewportPoint(worldPosition);
        var relativePos = new Vector2(viewportPos.x * 2 - 1, viewportPos.y * 2 - 1);

        var distanceScalar = Mathf.Clamp01(relativePos.magnitude / Mathf.Sqrt(2));
        distanceScalar = 1 - distanceScalar;
        distanceScalar *= distanceScalar;
        var durationScalar = distanceScalar * 0.5f + 0.5f;
        magnitude *= Mathf.Lerp(minScale, maxScale, distanceScalar);

        DoShake(relativePos.normalized, magnitude, duration * durationScalar);
    }

    public void DoShake(Vector2 direction, float magnitude, float duration)
    {
        // Add a new shake
        var shake = new ShakeInstance
        {
            maxDuration = duration,
            duration = 0,
            magnitude = magnitude,
            direction = direction
        };

        m_CurrentShakes.Add(shake);

        if (m_ShakeCounter == int.MaxValue)
            m_ShakeCounter = 0;
    }

    protected virtual void ProcessShake(ref ShakeInstance shake, ref Vector2 shakeVector)
    {
        if (shake.maxDuration > 0)
            shake.duration = Mathf.Clamp(shake.duration + Time.deltaTime, 0, shake.maxDuration);

        var settings = m_ShakingCamera.orthographic ? m_OrthographicSettings : m_PerspectiveSettings;
        var magnitude = CalculateShakeMagnitude(ref shake, settings);
        var additionalShake = CalculateRandomVector(ref shake, settings);

        shakeVector += additionalShake * magnitude;
    }


    private float CalculateShakeMagnitude(ref ShakeInstance shake, ShakeSettings currentSettings)
    {
        var t = shake.normalizedProgress;

        var noise = Mathf.PerlinNoise(Time.realtimeSinceStartup * m_MagnitudeNoiseScale, shake.duration);
        noise *= 0.6f + 0.4f;

        return Mathf.Lerp(shake.magnitude, 0, t) * noise * currentSettings.maxShake;
    }


    private Vector2 CalculateRandomVector(ref ShakeInstance shake, ShakeSettings currentSettings)
    {
        var noise = Mathf.PerlinNoise(Time.realtimeSinceStartup * m_DirectionNoiseScale, shake.duration);
        var deviation = noise * shake.magnitude * currentSettings.maxAngle;

        return shake.direction.Rotate(deviation);
    }

    [Serializable]
    public struct ShakeSettings
    {
        public float maxShake;
        public float maxAngle;
    }

    protected struct ShakeInstance
    {
        public float maxDuration, duration, magnitude;
        public Vector2 direction;

        public float normalizedProgress => Mathf.Clamp01(duration / maxDuration);

        public bool done => maxDuration > 0 && duration >= maxDuration - Mathf.Epsilon;
    }
}