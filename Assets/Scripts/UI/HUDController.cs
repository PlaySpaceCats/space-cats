using Tanks.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : Singleton<HUDController>
{
    private AudioSource m_AudioSource;

    [SerializeField] protected CanvasGroup m_DamageFlashGroup;

    [SerializeField] protected float m_DefaultOpacity = 0.3f;

    [SerializeField] protected float m_DesiredOpacity;

    private float m_FlashAlpha;
    private TankHealth m_Health;

    [SerializeField] protected Image m_HealthIcon;

    [SerializeField] protected float m_HealthPulseRate = 0.2f;

    private float m_HealthPulseScale = 1f;

    [Header("Health display")]
    [SerializeField]
    protected Image m_HealthSlider;

    [SerializeField] protected float m_HeldOpacity = 0.65f;

    private Canvas m_HudCanvas;

    [SerializeField] protected GameObject m_HudParent;

    [SerializeField] protected float m_MaxFlashAlpha = 0.5f;

    [SerializeField] protected float m_OpacityChangeSpeed = 2f;

    [SerializeField] protected float m_PulseScaleAdd = 0.5f;

    private AudioClip m_QueuedSound;

    private TankManager m_TankManager;

    [SerializeField] protected float m_VPadBuffer = 15;

    [Header("V-pad")] [SerializeField] protected Vector3 m_VPadDefault;

    [SerializeField] protected CanvasGroup m_VPadGroup;

    [SerializeField] protected GameObject m_VPadHeldPos;

    [SerializeField] protected GameObject m_VPadMain;

    public void UpdateVPad(Vector2 vPadCenter, Vector2 vPadHeldPosition, bool held)
    {
        if (m_VPadMain == null || m_VPadHeldPos == null)
        {
            return;
        }
        if (held)
        {
            m_VPadMain.transform.position = new Vector3(vPadCenter.x, vPadCenter.y, m_VPadDefault.z);
            m_VPadHeldPos.transform.position =
                new Vector3(vPadHeldPosition.x, vPadHeldPosition.y, m_VPadDefault.z - 0.01f);
        }
        else
        {
            m_VPadMain.transform.position = m_VPadDefault;
            m_VPadHeldPos.transform.position = m_VPadDefault;
        }
    }

    public void ShowVPad(float horizontalArea, float verticalArea)
    {
        if (m_VPadMain == null || m_VPadHeldPos == null || m_VPadGroup == null)
        {
            return;
        }
        m_VPadGroup.gameObject.SetActive(true);
        m_VPadGroup.alpha = m_DefaultOpacity;

        // Set size
        var canvasScale = m_HudCanvas.scaleFactor;
        var invCanvasScale = 1 / canvasScale;
        var canvasWidth = Screen.width * invCanvasScale;
        var canvasHeight = Screen.height * invCanvasScale;
        var desiredSize = Mathf.Min(canvasWidth * horizontalArea, canvasHeight * verticalArea) - m_VPadBuffer * 2;
        var desiredPos = Mathf.Min(Screen.width * horizontalArea, Screen.height * verticalArea) * 0.5f +
                         m_VPadBuffer;

        Vector2 desiredPosVector = new Vector3(desiredPos, desiredPos, 0);

        m_VPadDefault = desiredPosVector;
        m_VPadMain.transform.localPosition = desiredPosVector;
        m_VPadHeldPos.transform.localPosition = desiredPosVector;

        var rectTransform = m_VPadMain.transform as RectTransform;
        if (rectTransform != null) rectTransform.sizeDelta = new Vector2(desiredSize, desiredSize);
    }

    public void HideVPad()
    {
        if (m_VPadMain != null && m_VPadHeldPos != null) m_VPadGroup.gameObject.SetActive(false);
    }

    public void SetVPadHeld()
    {
        m_DesiredOpacity = m_HeldOpacity;
    }

    public void SetVPadReleased()
    {
        m_DesiredOpacity = m_DefaultOpacity;
    }

    protected void Start()
    {
        m_AudioSource = GetComponent<AudioSource>();

        m_HudCanvas = GetComponent<Canvas>();
    }

    protected void Update()
    {
        if (m_HealthPulseScale > 1f)
        {
            m_HealthPulseScale -= Time.deltaTime * m_HealthPulseRate;

            if (m_HealthPulseScale <= 1.01f) m_HealthPulseScale = 1f;

            m_HealthIcon.transform.localScale = Vector3.one * m_HealthPulseScale;
        }

        if (m_DamageFlashGroup.gameObject.activeSelf)
        {
            if (m_FlashAlpha > 0)
            {
                m_FlashAlpha -= Time.deltaTime;
                m_DamageFlashGroup.alpha = m_FlashAlpha;
            }
            else
            {
                m_DamageFlashGroup.gameObject.SetActive(false);
            }
        }

        if (m_VPadGroup == null)
        {
            return;
        }
        var currentOpacity = m_VPadGroup.alpha;
        var diff = m_DesiredOpacity - currentOpacity;
        var absDiff = Mathf.Abs(diff);

        if (absDiff <= Mathf.Epsilon)
        {
            return;
        }
        var changeAmount = Mathf.Sign(diff) *
                           Mathf.Min(Time.deltaTime * m_OpacityChangeSpeed, absDiff);

        m_VPadGroup.alpha += changeAmount;
    }

    private void UpdateHealth(float newHealthRatio)
    {
        m_HealthSlider.fillAmount = newHealthRatio;

        if (newHealthRatio == 1f)
        {
            return;
        }
        m_FlashAlpha = m_MaxFlashAlpha;
        m_DamageFlashGroup.gameObject.SetActive(true);

        m_HealthPulseScale = 1f + m_PulseScaleAdd;
    }

    private void OnPlayerDeath()
    {
        SetHudEnabled(false);
    }

    private void OnPlayerRespawn()
    {
        m_FlashAlpha = 0f;
        SetHudEnabled(true);
    }

    public void SetHudEnabled(bool enable)
    {
        if (m_HudParent != null)
            m_HudParent.SetActive(enable);
        else
            m_HudCanvas.enabled = enable;
    }

    public void InitHudPlayer(TankHealth health)
    {
        m_Health = health;

        m_Health.healthChanged += UpdateHealth;
        m_Health.playerDeath += OnPlayerDeath;
        m_Health.playerReset += OnPlayerRespawn;

        SetHudEnabled(true);
    }

    protected override void OnDestroy()
    {
        if (m_Health != null)
        {
            m_Health.healthChanged -= UpdateHealth;
            m_Health.playerDeath -= OnPlayerDeath;
            m_Health.playerReset -= OnPlayerRespawn;
        }

        base.OnDestroy();
    }

    public void PlayInterfaceAudio(AudioClip soundEffect)
    {
        m_AudioSource.PlayOneShot(soundEffect);
    }
}