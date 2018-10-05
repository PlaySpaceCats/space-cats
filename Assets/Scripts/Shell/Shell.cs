using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Shell : MonoBehaviour
{
    [SerializeField] protected Vector3 m_BounceAdditionalForce = Vector3.up;

    [SerializeField] protected ExplosionSettings m_BounceExplosionSettings;

    [SerializeField] protected float m_BounceForceDecay = 1.05f;

    [SerializeField] protected int m_Bounces;

    private Rigidbody m_CachedRigidBody;

    private float m_CurrentSpinRot;

    private bool m_Exploded;

    [SerializeField] protected ExplosionSettings m_ExplosionSettings;

    [SerializeField] protected int m_IgnoreColliderFixedFrames = 2;

    [SerializeField] protected float m_MinY = -1;

    private TrailRenderer[] m_ShellTrails;

    [SerializeField] protected float m_SpeedModifier = 1;

    [SerializeField] protected float m_Spin = 720;

    private Collider m_TempIgnoreCollider;
    private int m_TempIgnoreColliderTime = 2;

    public float speedModifier => m_SpeedModifier;

    private void Awake()
    {
        m_CachedRigidBody = GetComponent<Rigidbody>();
        m_Exploded = false;

        m_ShellTrails = GetComponentsInChildren<TrailRenderer>();
    }

    public void Setup()
    {
        if (m_SpeedModifier != 1)
        {
            var force = gameObject.AddComponent<ConstantForce>();
            force.force = Physics.gravity * m_CachedRigidBody.mass * (m_SpeedModifier - 1);
        }

        transform.forward = m_CachedRigidBody.velocity;
    }

    private void FixedUpdate()
    {
        if (m_TempIgnoreCollider == null)
        {
            return;
        }
        m_TempIgnoreColliderTime--;
        if (m_TempIgnoreColliderTime > 0)
        {
            return;
        }
        Physics.IgnoreCollision(GetComponent<Collider>(), m_TempIgnoreCollider, false);
        m_TempIgnoreCollider = null;
    }

    private void Update()
    {
        transform.forward = m_CachedRigidBody.velocity;

        m_CurrentSpinRot += m_Spin * Time.deltaTime * m_CachedRigidBody.velocity.magnitude;
        transform.Rotate(Vector3.forward, m_CurrentSpinRot, Space.Self);

        if (transform.position.y <= m_MinY)
            Destroy(gameObject);
        else
            m_Exploded = false;
    }

    private void OnCollisionEnter(Collision c)
    {
        if (m_Exploded) return;

        var explosionNormal = c.contacts.Length > 0 ? c.contacts[0].normal : Vector3.up;
        var settings = m_Bounces > 0 ? m_BounceExplosionSettings : m_ExplosionSettings;

        if (ExplosionManager.Instance != null)
        {
            var em = ExplosionManager.Instance;
            if (settings != null) em.SpawnExplosion(transform.position, explosionNormal, gameObject, settings, false);
        }

        if (m_Bounces > 0)
        {
            m_Bounces--;

            var refl = Vector3.Reflect(-c.relativeVelocity, explosionNormal);
            refl *= m_BounceForceDecay;
            refl += m_BounceAdditionalForce;
            m_CachedRigidBody.velocity = refl;

            if (m_TempIgnoreCollider != null)
                Physics.IgnoreCollision(GetComponent<Collider>(), m_TempIgnoreCollider, false);
            m_TempIgnoreCollider = c.collider;
            m_TempIgnoreColliderTime = m_IgnoreColliderFixedFrames;
        }
        else
        {
            Destroy(gameObject);
        }

        m_Exploded = true;
    }

    private void OnDestroy()
    {
        foreach (var trail in m_ShellTrails)
        {
            if (trail == null)
            {
                continue;
            }
            trail.transform.SetParent(null);
            trail.autodestruct = true;
        }
    }
}