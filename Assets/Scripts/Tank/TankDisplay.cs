using UnityEngine;

public class TankDisplay : MonoBehaviour
{
    [SerializeField] private Transform _backWheels;

    [SerializeField] private Transform _frontWheels;

    private TankMovement _movement;

    [SerializeField] protected float m_BobFrequency;

    [SerializeField] protected float m_BobNoiseScale;

    private DamageOutlineFlash m_DamageFlash;

    [SerializeField] protected Transform[] m_DustTrails;

    [SerializeField] protected Transform m_FireTransform;

    [SerializeField] protected float m_IdleShakeMagnitude;

    [SerializeField] protected float m_IdleShakeNoiseScale;

    [SerializeField] protected float m_MovingShakeMagnitude;

    [SerializeField] protected float m_MovingShakeNoiseScale;

    [SerializeField] protected MeshRenderer m_Shadow;

    [SerializeField] protected Vector3 m_ShakeDirections;

    [SerializeField] protected GameObject m_TankRendererParent;

    [SerializeField] protected Renderer[] m_TankRenderers;

    private ParticleSystem[] m_TrackTrailParticles;

    [SerializeField] protected Transform m_TurretTransform;

    private void Awake()
    {
        m_DamageFlash = GetComponent<DamageOutlineFlash>();
    }

    private void Start()
    {
        var trailObject = ThemedEffectsLibrary.Instance.GetTrackParticlesForMap();

        m_TrackTrailParticles = new ParticleSystem[m_DustTrails.Length];

        for (var i = 0; i < m_DustTrails.Length; i++)
        {
            var newTrailEffect = Instantiate(trailObject, m_DustTrails[i].position, m_DustTrails[i].rotation,
                m_DustTrails[i]);
            newTrailEffect.transform.localScale = Vector3.one;
            m_TrackTrailParticles[i] = newTrailEffect.transform.GetComponent<ParticleSystem>();
            m_TrackTrailParticles[i].Play();
        }
    }

    private void Update()
    {
        RotateWheels();
        DoShake();
    }

    public void Init(TankMovement movement, Material material)
    {
        _movement = movement;
        m_TankRenderers[0].material = material;
        var materials = m_TankRenderers[1].materials;
        materials[1] = material;
        m_TankRenderers[1].materials = materials;
    }

    public Transform GetFireTransform()
    {
        return m_FireTransform;
    }

    public Transform GetTurretTransform()
    {
        return m_TurretTransform;
    }

    public void StopTrackParticles()
    {
        if (m_TrackTrailParticles == null)
        {
            return;
        }
        foreach (var particle in m_TrackTrailParticles)
        {
            particle.Clear();
            particle.Stop();
        }
    }

    public void ReEnableTrackParticles()
    {
        if (m_TrackTrailParticles == null)
        {
            return;
        }

        foreach (var particle in m_TrackTrailParticles)
        {
            particle.Play();
        }
    }

    public void SetVisibleObjectsActive(bool active)
    {
        if (!active) StopTrackParticles();

        SetGameObjectActive(m_TankRendererParent, active);
        if (m_Shadow != null) SetGameObjectActive(m_Shadow.gameObject, active);
    }

    private static void SetGameObjectActive(GameObject gameObject, bool active)
    {
        if (gameObject == null) return;

        gameObject.SetActive(active);
    }

    public void HideShadow()
    {
        m_Shadow.gameObject.SetActive(false);
    }

    public void StartDamageFlash()
    {
        m_DamageFlash.StartDamageFlash();
    }

    private void RotateWheels()
    {
        if (_movement == null) return;

        if (!_movement.isMoving) return;

        var delta = _movement.currentMovementMode == TankMovement.MovementMode.Forward ? 300 : -300;
        _frontWheels.Rotate(delta * Time.deltaTime, 0, 0);
        _backWheels.Rotate(delta * Time.deltaTime, 0, 0);
    }

    private void DoShake()
    {
        if (_movement == null) return;

        var moving = _movement.isMoving;
        var shakeMagnitude = moving ? m_MovingShakeMagnitude : m_IdleShakeMagnitude;
        var shakeScale = moving ? m_MovingShakeNoiseScale : m_IdleShakeNoiseScale;

        var xNoise = (Mathf.PerlinNoise((Time.realtimeSinceStartup + 0) * shakeScale, Time.smoothDeltaTime) * 2 - 1) *
                     shakeMagnitude;
        var zNoise = (Mathf.PerlinNoise((Time.realtimeSinceStartup + 100) * shakeScale, Time.smoothDeltaTime) * 2 - 1) *
                     shakeMagnitude;

        var yNoise = Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * Mathf.PI * m_BobFrequency)) * shakeMagnitude *
                     Mathf.PerlinNoise((Time.realtimeSinceStartup + 50) * m_BobNoiseScale, Time.smoothDeltaTime);

        var offset = Vector3.Scale(m_ShakeDirections, new Vector3(xNoise, yNoise, zNoise));
        m_TankRendererParent.transform.localPosition = offset;
    }
}