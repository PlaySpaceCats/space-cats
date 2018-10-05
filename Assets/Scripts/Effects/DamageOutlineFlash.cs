using UnityEngine;

public class DamageOutlineFlash : MonoBehaviour
{
    private Material m_BorderBaseMaterial;

    private float m_BorderBaseThickness;

    [SerializeField] protected Renderer[] m_BorderRenderers;

    [SerializeField] protected float m_DamageBorderPulseAmount = 4f;

    [SerializeField] protected Color m_DamageColor = Color.red;

    [SerializeField] protected float m_DamageFadeDuration = 0.15f;

    private float m_DamageFadeTime;

    public void StartDamageFlash()
    {
        m_DamageFadeTime = m_DamageFadeDuration;
    }

    private void Awake()
    {
        if (m_BorderRenderers.Length <= 0)
        {
            return;
        }

        m_BorderBaseMaterial = m_BorderRenderers[0].sharedMaterial;

        m_BorderBaseThickness = m_BorderBaseMaterial.GetFloat("_OutlineWidth");
    }

    private void Update()
    {
        if (m_DamageFadeTime <= 0f)
        {
            return;
        }

        m_DamageFadeTime -= Time.deltaTime;

        foreach (var border in m_BorderRenderers)
        {
            border.material.color =
                Color.Lerp(Color.black, m_DamageColor, m_DamageFadeTime / m_DamageFadeDuration);

            border.material.SetFloat("_OutlineWidth",
                Mathf.Lerp(m_BorderBaseThickness, m_DamageBorderPulseAmount,
                    m_DamageFadeTime / m_DamageFadeDuration));
        }

        if (m_DamageFadeTime > 0f)
        {
            return;
        }

        foreach (var border in m_BorderRenderers)
        {
            Destroy(border.material);
            border.material = m_BorderBaseMaterial;
        }
    }
}