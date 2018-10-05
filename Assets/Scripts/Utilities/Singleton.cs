using UnityEngine;
using System.Collections;
using System;

namespace Tanks.Utilities
{
	public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
	{
		public static T Instance { get; private set; }
		
		public static event Action InstanceSet;

		protected virtual void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
			}
			else
			{
				Instance = (T) this;
				InstanceSet?.Invoke();
			}
		}

		protected virtual void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}
	}
}
