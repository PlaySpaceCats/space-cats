using System;
using UnityEngine;

public class TankHealth : MonoBehaviour, IDamageObject
{
    [SerializeField] protected GameObject m_AimCanvas;

    private BoxCollider m_Collider;

    private float m_CurrentHealth;

    private SpawnPoint m_CurrentSpawnPoint;

    [SerializeField] protected ExplosionSettings m_DeathExplosion;

    private TankManager m_Manager;
    private string m_PlayerId;
    private readonly float m_StartingHealth = 100f;
    private TankDisplay m_TankDisplay;

    private bool m_ZeroHealthHappened;

    public bool invulnerable { get; set; }

    public SpawnPoint currentSpawnPoint
    {
        set { m_CurrentSpawnPoint = value; }
    }

    public bool IsAlive => m_CurrentHealth > 0;

    public void Damage(float amount)
    {
        if (invulnerable) return;

        m_TankDisplay.StartDamageFlash();

        m_CurrentHealth -= amount;
        OnCurrentHealthChanged();

        if (m_CurrentHealth <= 0f && !m_ZeroHealthHappened) OnZeroHealth();
    }

    public event Action<float> healthChanged;
    public event Action playerDeath;
    public event Action playerReset;

    public void NullifySpawnPoint(SpawnPoint point)
    {
        if (m_CurrentSpawnPoint == point) m_CurrentSpawnPoint = null;
    }

    private void OnCurrentHealthChanged()
    {
        healthChanged?.Invoke(m_CurrentHealth / m_StartingHealth);
    }

    private void OnZeroHealth()
    {
        m_ZeroHealthHappened = true;

        if (ExplosionManager.Instance != null && m_DeathExplosion != null)
            ExplosionManager.Instance.SpawnExplosion(transform.position, Vector3.up, gameObject, m_DeathExplosion,
                false);

        SetTankActive(false);

        if (m_CurrentSpawnPoint != null) m_CurrentSpawnPoint.Decrement();

        playerDeath?.Invoke();

        m_Manager.Respawn();
    }

    public void Init(TankManager manager, TankDisplay display)
    {
        m_Manager = manager;
        m_TankDisplay = display;
        m_Collider = m_TankDisplay.GetComponent<BoxCollider>();

        SetDefaults();
    }

    private void SetTankActive(bool active)
    {
        if (m_Collider == null && m_TankDisplay != null) m_Collider = m_TankDisplay.GetComponent<BoxCollider>();
        if (m_Collider != null) m_Collider.enabled = active;

        m_TankDisplay.SetVisibleObjectsActive(active);

        m_AimCanvas.SetActive(active);

        if (active)
            m_Manager.EnableControl();
        else
            m_Manager.DisableControl();
    }

    public void SetDefaults()
    {
        m_CurrentHealth = m_StartingHealth;
        OnCurrentHealthChanged();
        m_ZeroHealthHappened = false;
        SetTankActive(true);

        playerReset?.Invoke();
    }
}