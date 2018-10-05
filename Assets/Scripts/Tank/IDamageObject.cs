public interface IDamageObject
{
	bool IsAlive { get; }

	void Damage(float damage);
}