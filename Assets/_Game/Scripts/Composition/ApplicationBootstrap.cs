using System.Collections;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class ApplicationBootstrap : MonoBehaviour
	{
		private EidrenServiceRoot _services;

		private void Awake()
		{
			_services = EidrenServiceRoot.FindOrCreate();
		}

		private IEnumerator Start()
		{
			yield return null;
			_services.SceneFlowService.TryLoadScene("MainMenu");
		}
	}
}
