using System.Collections;

namespace Eidren.Core.Services
{
	public interface ISceneLoader
	{
		string ActiveSceneName { get; }

		bool CanLoad(string sceneName);

		IEnumerator LoadAsync(string sceneName);
	}
}
