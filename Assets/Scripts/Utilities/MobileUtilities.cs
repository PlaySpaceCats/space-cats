using UnityEngine;

namespace Tanks.Utilities
{
	public static class MobileUtilities
	{
		public static bool IsOnMobile()
		{
#if UNITY_ANDROID || UNITY_IOS
			// True if not in editor
			return !Application.isEditor;
#else
			return false;
			#endif
		}
	}
}