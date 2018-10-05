using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsAffected : MonoBehaviour
{
	[SerializeField]
	private float m_UpwardsModifier;
	private Rigidbody _rigidbody;

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
	}
	
	public void ApplyForce(float force, Vector3 position, float radius)
	{
		_rigidbody.AddExplosionForce(force, position, radius, m_UpwardsModifier);
	}
}
