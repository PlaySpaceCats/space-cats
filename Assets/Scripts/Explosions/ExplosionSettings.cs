using UnityEngine;

public enum ExplosionClass
{
    Large,
    Small,
    ExtraLarge,
    TankExplosion,
    TurretExplosion,
    BounceExplosion,
    FiringExplosion
}

[CreateAssetMenu(fileName = "Explosion", menuName = "Explosion Definition", order = 1)]
public class ExplosionSettings : ScriptableObject
{
    public float damage;
    public ExplosionClass explosionClass;
    public float explosionRadius;
    public string id;
    public float physicsForce;
    public float physicsRadius;

    [Range(0, 1)] public float shakeMagnitude;
}