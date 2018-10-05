using System.Linq;
using UnityEngine;

public class Effect : MonoBehaviour
{
    [SerializeField] protected bool m_AutoDestroy;

    public void Awake()
    {
        float lifetime = 0;
        var systems = GetComponentsInChildren<ParticleSystem>();

        if (systems != null)
        {
            foreach (var system in systems)
            {
                system.Play();
                lifetime = Mathf.Max(system.main.duration, lifetime);
            }
        }

        var sources = GetComponentsInChildren<AudioSource>();
        if (sources != null)
        {
            lifetime = sources.Where(source => source.clip != null).Aggregate(lifetime, (current, source) => Mathf.Max(current, source.clip.length));
        }

        if (m_AutoDestroy)
        {
            Destroy(gameObject, lifetime);
        }
    }
}