using Tanks.Utilities;
using UnityEngine;
using UnityEngine.Networking;

public class ExplosionManager : Singleton<ExplosionManager>
{
	private EffectsGroup m_EffectsGroup;

	[SerializeField] protected float m_ExplosionScreenShakeDuration = 0.3f;

	private int m_PhysicsMask;

	protected override void Awake()
    {
        base.Awake();

        m_PhysicsMask = LayerMask.GetMask("Players", "Projectiles", "Powerups", "DestructibleHazards", "Decorations");
    }

    protected virtual void Start()
    {
        m_EffectsGroup = ThemedEffectsLibrary.Instance.GetEffectsGroupForMap();
    }

	public void SpawnExplosion(Vector3 explosionPosition, Vector3 explosionNormal, GameObject ignoreObject,
        ExplosionSettings explosionConfig, bool clientOnly)
    {
        CreateVisualExplosion(explosionPosition, explosionNormal, explosionConfig.explosionClass);

        DoLogicalExplosion(explosionPosition, ignoreObject, explosionConfig);
    }

	private void DoLogicalExplosion(Vector3 explosionPosition, GameObject ignoreObject,
        ExplosionSettings explosionConfig)
    {
        var colliders = Physics.OverlapSphere(explosionPosition,
            Mathf.Max(explosionConfig.explosionRadius, explosionConfig.physicsRadius), m_PhysicsMask);

        foreach (var struckCollider in colliders)
        {
            if (struckCollider.gameObject == ignoreObject)
            {
                continue;
            }

            var explosionToTarget = struckCollider.transform.position - explosionPosition;

            var explosionDistance = explosionToTarget.magnitude;

            var targetHealth = struckCollider.GetComponentInParent<IDamageObject>();

            if (targetHealth != null &&
                targetHealth.IsAlive &&
                explosionDistance < explosionConfig.explosionRadius)
            {
                var normalizedDistance =
                    Mathf.Clamp01((explosionConfig.explosionRadius - explosionDistance) /
                                  explosionConfig.explosionRadius);

                var damage = normalizedDistance * explosionConfig.damage;

                targetHealth.Damage(damage);
            }

            var physicsObject = struckCollider.GetComponentInParent<PhysicsAffected>();
            var identity = struckCollider.GetComponentInParent<NetworkIdentity>();

            if (physicsObject != null && physicsObject.enabled && explosionDistance < explosionConfig.physicsRadius &&
                (identity == null || identity.hasAuthority))
                physicsObject.ApplyForce(explosionConfig.physicsForce, explosionPosition,
                    explosionConfig.physicsRadius);
        }

        DoShakeForExplosion(explosionPosition, explosionConfig);
    }


    private void CreateVisualExplosion(Vector3 explosionPosition, Vector3 explosionNormal,
        ExplosionClass explosionClass)
    {
        Effect spawnedEffect;

        switch (explosionClass)
        {
            case ExplosionClass.ExtraLarge:
                spawnedEffect = Instantiate(m_EffectsGroup.extraLargeExplosion);
                break;
            case ExplosionClass.Large:
                spawnedEffect = Instantiate(m_EffectsGroup.largeExplosion);
                break;
            case ExplosionClass.Small:
                spawnedEffect = Instantiate(m_EffectsGroup.smallExplosion);
                break;
            case ExplosionClass.TankExplosion:
                spawnedEffect = Instantiate(m_EffectsGroup.tankExplosion);
                break;
            case ExplosionClass.TurretExplosion:
                spawnedEffect = Instantiate(m_EffectsGroup.turretExplosion);
                break;
            case ExplosionClass.BounceExplosion:
                spawnedEffect = Instantiate(m_EffectsGroup.smallExplosion);
                break;
            case ExplosionClass.FiringExplosion:
                spawnedEffect = Instantiate(m_EffectsGroup.firingExplosion);
                break;
            default:
                spawnedEffect = null;
                break;
        }

        if (spawnedEffect == null)
        {
            return;
        }

        spawnedEffect.transform.position = explosionPosition;
        spawnedEffect.transform.up = explosionNormal;

        var sound = spawnedEffect.GetComponentInChildren<AudioSource>();
        if (sound == null)
        {
            return;
        }
        sound.clip = ThemedEffectsLibrary.Instance.GetRandomExplosionSound(explosionClass);
        sound.Play();
    }

    private void DoShakeForExplosion(Vector3 explosionPosition, ExplosionSettings explosionConfig)
    {
        if (ScreenShakeController.Instance == null)
        {
            return;
        }

        var shaker = ScreenShakeController.Instance;

        var shakeMagnitude = explosionConfig.shakeMagnitude;
        shaker.DoShake(explosionPosition, shakeMagnitude, m_ExplosionScreenShakeDuration, 0.0f, 1.0f);
    }
}