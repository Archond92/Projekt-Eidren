using System.Collections;

namespace Eidren.Core.Services
{
	public interface ISceneTransitionView
	{
		void SetInputBlocked(bool blocked);

		IEnumerator FadeOut();

		IEnumerator FadeIn();
	}
}
