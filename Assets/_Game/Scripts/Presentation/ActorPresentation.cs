using UnityEngine;

namespace Eidren.Presentation
{
	public static class ActorPresentationLocator
	{
		public static IActorPresentation Find(GameObject root)
		{
			if (root == null)
			{
				return null;
			}
			MonoBehaviour[] componentsInChildren = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
			MonoBehaviour[] array = componentsInChildren;
			foreach (MonoBehaviour monoBehaviour in array)
			{
				if (monoBehaviour is IActorPresentation result)
				{
					return result;
				}
			}
			return null;
		}
	}
}
