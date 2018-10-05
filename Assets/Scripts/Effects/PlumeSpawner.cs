using UnityEngine;

public class PlumeSpawner : MonoBehaviour
{
    private ParticleSystem m_CachedParticleSystem;

    [SerializeField] protected float m_DistanceBetweenParticles;

    private ParticleSystem.EmitParams m_EmitParams;
    private ParticleSystem.Particle[] m_ParticleSpawners;

    [SerializeField] protected ParticleSystem m_PlumeParticles;

    [SerializeField] protected float m_PosDeviation;

    private Vector3[] m_PrevPosition;

    private void Start()
    {
        m_EmitParams = new ParticleSystem.EmitParams();
    }

    private void Init()
    {
        if (m_CachedParticleSystem == null) m_CachedParticleSystem = GetComponent<ParticleSystem>();

        if (m_ParticleSpawners == null || m_ParticleSpawners.Length < m_CachedParticleSystem.main.maxParticles)
            m_ParticleSpawners = new ParticleSystem.Particle[m_CachedParticleSystem.main.maxParticles];

        if (m_PrevPosition == null || m_PrevPosition.Length != m_CachedParticleSystem.main.maxParticles)
            m_PrevPosition = new Vector3[m_CachedParticleSystem.main.maxParticles];
    }

    private void LateUpdate()
    {
        Init();

        var numParticlesAlive = m_CachedParticleSystem.GetParticles(m_ParticleSpawners);

        for (var i = 0; i < numParticlesAlive; i++)
        {
            var p = m_ParticleSpawners[i];
            if (Vector3.Distance(p.position, m_PrevPosition[i]) > m_DistanceBetweenParticles)
            {
                m_EmitParams.startSize = p.GetCurrentSize(m_CachedParticleSystem);
                m_EmitParams.position = p.position + Random.insideUnitSphere * m_PosDeviation;
                m_PlumeParticles.Emit(m_EmitParams, 1);
            }

            m_PrevPosition[i] = p.position;
        }

        m_CachedParticleSystem.SetParticles(m_ParticleSpawners, numParticlesAlive);
    }
}