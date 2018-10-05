using UnityEngine;
using UnityEngine.UI;

public class TankShooting : MonoBehaviour
{
    [SerializeField] protected Slider m_AimSlider;

    [SerializeField] protected Transform m_AimSliderParent;

    [SerializeField] protected float m_ChargeShakeMagnitude;

    [SerializeField] protected float m_ChargeShakeNoiseScale;

    private float m_ChargeSpeed;

    [SerializeField] protected AudioClip m_ChargingClip;

    private float m_ClientTurretHeading;
    private float m_ClientTurretHeadingVel;

    private float m_CurrentLaunchAngle;

    private Vector3 m_DefaultTurretPos;

    [SerializeField] protected AudioClip[] m_FireClip;

    private bool m_Fired;

    private bool m_FireInput;

    [SerializeField] protected AnimationCurve m_FireRecoilCurve;

    [SerializeField] protected float m_FireRecoilMagnitude = 0.25f;

    [SerializeField] protected float m_FireRecoilSpeed = 4f;

    private Transform m_FireTransform;

    [SerializeField] protected ExplosionSettings m_FiringExplosion;

    [SerializeField] protected AudioSource m_HooterAudio;

    private bool m_Initialized;

    private float m_LastLookUpdate;

    [SerializeField] protected float m_LookDirTickInterval;

    [SerializeField] protected float m_MaxChargeTime = 0.75f;

    [SerializeField] protected float m_MaxLaunchAngle = 70f;

    private int m_MeepIndex;

    [SerializeField] protected AudioClip[] m_MeepSounds;

    [SerializeField] protected float m_MinimumSafetyRange = 4f;

    [SerializeField] protected float m_MinLaunchAngle = 20f;

    private Vector2 m_RecoilDirection;
    private float m_RecoilTime;

    private readonly float m_RefireRate = 0.6f;

    private float m_ReloadTime;

    [SerializeField] protected Rigidbody m_Shell;

    [SerializeField] protected AudioSource m_ShootingAudio;

    [SerializeField] protected float m_ShootShakeDuration;

    [SerializeField] protected float m_ShootShakeMaxMagnitude;

    [SerializeField] protected float m_ShootShakeMinMagnitude;

    private float m_SqrMinimumSafetyRange;
    private float m_SqrTargetRange;

    private Vector3 m_TargetFirePosition;

    private float m_TurretHeading;

    private Transform m_TurretTransform;
    private bool m_WasFireInput;

    private static TankShooting s_LocalTank { get; set; }

    public bool canShoot { private get; set; }

    public Vector3 GetDesiredFirePosition()
    {
        return m_TargetFirePosition;
    }

    public void SetDesiredFirePosition(Vector3 target)
    {
        m_TargetFirePosition = target;

        var toAimPos = m_TargetFirePosition - transform.position;
        m_TurretHeading = 90 - Mathf.Atan2(toAimPos.z, toAimPos.x) * Mathf.Rad2Deg;

        if (Time.realtimeSinceStartup - m_LastLookUpdate >= m_LookDirTickInterval)
        {
            m_LastLookUpdate = Time.realtimeSinceStartup;
        }

        m_SqrTargetRange = toAimPos.sqrMagnitude;
    }

    public void SetFireIsHeld(bool fireHeld)
    {
        m_FireInput = fireHeld;
    }

    public bool GetFireIsHeld()
    {
        return m_FireInput;
    }

    private void Awake()
    {
        m_LastLookUpdate = Time.realtimeSinceStartup;
    }

    private void Start()
    {
        m_ChargeSpeed = (m_MaxLaunchAngle - m_MinLaunchAngle) / m_MaxChargeTime;

        m_SqrMinimumSafetyRange = Mathf.Pow(m_MinimumSafetyRange, 2f);

        m_MeepIndex = Random.Range(0, m_MeepSounds.Length);
    }

    private void OnDisable()
    {
        SetDefaults();
    }

    private void Update()
    {
        if (!m_Initialized) return;

        m_ClientTurretHeading = Mathf.SmoothDampAngle(m_ClientTurretHeading, m_TurretHeading, ref m_ClientTurretHeadingVel, Time.deltaTime);
        m_TurretTransform.rotation = Quaternion.AngleAxis(m_ClientTurretHeading, Vector3.up);

        if (s_LocalTank == null) s_LocalTank = this;

        if (m_ReloadTime > 0) m_ReloadTime -= Time.deltaTime;

        if (m_FireInput && !m_WasFireInput && InSafetyRange())
        {
            m_ShootingAudio.Stop();

            if (!m_HooterAudio.isPlaying)
            {
                m_HooterAudio.clip = m_MeepSounds[m_MeepIndex];
                m_HooterAudio.Play();
            }
        }
        else if (m_CurrentLaunchAngle <= m_MinLaunchAngle && !m_Fired)
        {
            m_CurrentLaunchAngle = m_MinLaunchAngle;
            Fire();
        }
        else if (m_FireInput && !m_WasFireInput && CanFire())
        {
            m_Fired = false;

            m_CurrentLaunchAngle = m_MaxLaunchAngle;

            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }
        else if (m_FireInput && !m_Fired)
        {
            m_CurrentLaunchAngle -= m_ChargeSpeed * Time.deltaTime;
        }
        else if (!m_FireInput && m_WasFireInput && !m_Fired)
        {
            Fire();
        }

        m_WasFireInput = m_FireInput;

        UpdateAimSlider();

        var shakeMagnitude = Mathf.Lerp(0, m_ChargeShakeMagnitude,
            Mathf.InverseLerp(m_MaxLaunchAngle, m_MinLaunchAngle, m_CurrentLaunchAngle));
        var shakeOffset = Vector2.zero;

        if (shakeMagnitude > 0)
        {
            shakeOffset.x =
                (Mathf.PerlinNoise((Time.realtimeSinceStartup + 0) * m_ChargeShakeNoiseScale, Time.smoothDeltaTime) *
                 2 - 1) * shakeMagnitude;
            shakeOffset.y =
                (Mathf.PerlinNoise((Time.realtimeSinceStartup + 100) * m_ChargeShakeNoiseScale, Time.smoothDeltaTime) *
                 2 - 1) * shakeMagnitude;
        }

        if (m_RecoilTime > 0)
        {
            m_RecoilTime = Mathf.Clamp01(m_RecoilTime - Time.deltaTime * m_FireRecoilSpeed);
            var recoilPoint = m_FireRecoilCurve.Evaluate(1 - m_RecoilTime);

            shakeOffset += m_RecoilDirection * recoilPoint * m_FireRecoilMagnitude;
        }

        m_TurretTransform.localPosition = m_DefaultTurretPos + new Vector3(shakeOffset.x, 0, shakeOffset.y);
    }

    private bool CanFire()
    {
        return m_ReloadTime <= 0 && canShoot;
    }

    private bool InSafetyRange()
    {
        return m_SqrTargetRange <= m_SqrMinimumSafetyRange;
    }

    private void Fire()
    {
        m_Fired = true;

        var shellToFire = m_Shell.GetComponent<Shell>();

        var fireVector = FiringLogic.CalculateFireVector(shellToFire, m_TargetFirePosition, m_FireTransform.position,
            m_CurrentLaunchAngle);

        FireVisualClientShell(fireVector, m_FireTransform.position);

        m_CurrentLaunchAngle = m_MaxLaunchAngle;

        m_ReloadTime = m_RefireRate;

        if (ScreenShakeController.Instance != null)
        {
            var shaker = ScreenShakeController.Instance;

            var chargeAmount = Mathf.InverseLerp(m_MaxLaunchAngle, m_MinLaunchAngle, m_CurrentLaunchAngle);
            var magnitude = Mathf.Lerp(m_ShootShakeMinMagnitude, m_ShootShakeMaxMagnitude, chargeAmount);
            shaker.DoShake(m_TargetFirePosition, magnitude, m_ShootShakeDuration);
        }

        m_RecoilTime = 1;
        var localVector = transform.InverseTransformVector(fireVector);
        m_RecoilDirection = new Vector2(-localVector.x, -localVector.z);
    }

    private void FireVisualClientShell(Vector3 shotVector, Vector3 position)
    {
        if (ExplosionManager.Instance != null)
            ExplosionManager.Instance.SpawnExplosion(position, shotVector, null, m_FiringExplosion, true);

        var shellInstance =
            Instantiate(m_Shell);

        shellInstance.transform.position = position;
        shellInstance.velocity = shotVector;

        var shell = shellInstance.GetComponent<Shell>();
        shell.Setup();

        Physics.IgnoreCollision(shell.GetComponent<Collider>(), GetComponentInChildren<Collider>(), true);

        m_ShootingAudio.clip = m_FireClip[Random.Range(0, m_FireClip.Length)];

        m_ShootingAudio.Play();
    }

    public void Init(TankDisplay display)
    {
        enabled = false;
        canShoot = false;
        m_Initialized = true;
        m_FireTransform = display.GetFireTransform();
        m_TurretTransform = display.GetTurretTransform();

        m_AimSliderParent.SetParent(m_TurretTransform, false);
        m_DefaultTurretPos = m_TurretTransform.localPosition;

        SetDefaults();
    }

    private void UpdateAimSlider()
    {
        var aimValue = m_Fired ? m_MaxLaunchAngle : m_CurrentLaunchAngle;
        m_AimSlider.value = m_MaxLaunchAngle - aimValue + m_MinLaunchAngle;
    }

    public void SetDefaults()
    {
        enabled = true;
        m_CurrentLaunchAngle = m_MaxLaunchAngle;
        UpdateAimSlider();
        m_FireInput = m_WasFireInput = false;
        m_Fired = true;
        m_ShootingAudio.Stop();
        m_HooterAudio.Stop();
    }
}