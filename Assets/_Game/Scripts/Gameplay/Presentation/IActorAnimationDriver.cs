using UnityEngine;

namespace Eidren.Gameplay.Presentation
{
	public interface IActorAnimationDriver
	{
		void PlayHit();

		void PlayAbility(int skillIndex);

		void PlayCompanionAttack();
	}

	public static class ActorAnimationDriverLocator
	{
		public static IActorAnimationDriver Find(GameObject root)
		{
			if (root == null)
			{
				return null;
			}
			MonoBehaviour[] componentsInChildren = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
			MonoBehaviour[] array = componentsInChildren;
			foreach (MonoBehaviour monoBehaviour in array)
			{
				if (monoBehaviour is IActorAnimationDriver result)
				{
					return result;
				}
			}
			return null;
		}
	}
}
