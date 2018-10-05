using System;
using Tanks.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct EffectsGroup
{
    public Effect smallExplosion;
    public AudioClip[] smallExplosionSounds;

    public Effect largeExplosion;
    public AudioClip[] largeExplosionSounds;

    public Effect extraLargeExplosion;
    public AudioClip[] extraLargeExplosionSounds;

    public Effect tankExplosion;
    public AudioClip[] tankExplosionSounds;

    public Effect turretExplosion;
    public AudioClip[] turretExplosionSounds;

    public AudioClip[] bouncyBombExplosionSounds;

    public Effect firingExplosion;

    public GameObject tankTrackParticles;
}

public class ThemedEffectsLibrary : PersistentSingleton<ThemedEffectsLibrary>
{
    private readonly int m_ActiveEffectsGroup = 0;

    [SerializeField] protected EffectsGroup[] m_EffectsGroups;

    public EffectsGroup GetEffectsGroupForMap()
    {
        return m_EffectsGroups[m_ActiveEffectsGroup];
    }

    public GameObject GetTrackParticlesForMap()
    {
        return m_EffectsGroups[m_ActiveEffectsGroup].tankTrackParticles;
    }

    public AudioClip GetRandomExplosionSound(ExplosionClass explosionType)
    {
        var activeGroup = m_EffectsGroups[m_ActiveEffectsGroup];

        switch (explosionType)
        {
            case ExplosionClass.Small:
                return activeGroup.smallExplosionSounds[Random.Range(0, activeGroup.smallExplosionSounds.Length)];

            case ExplosionClass.Large:
                return activeGroup.largeExplosionSounds[Random.Range(0, activeGroup.largeExplosionSounds.Length)];

            case ExplosionClass.ExtraLarge:
                return activeGroup.extraLargeExplosionSounds[
                    Random.Range(0, activeGroup.extraLargeExplosionSounds.Length)];

            case ExplosionClass.BounceExplosion:
                return activeGroup.bouncyBombExplosionSounds[
                    Random.Range(0, activeGroup.bouncyBombExplosionSounds.Length)];

            case ExplosionClass.TankExplosion:
                return activeGroup.tankExplosionSounds[Random.Range(0, activeGroup.tankExplosionSounds.Length)];

            case ExplosionClass.TurretExplosion:
                return activeGroup.turretExplosionSounds[Random.Range(0, activeGroup.turretExplosionSounds.Length)];

            default:
                return null;
        }
    }
}