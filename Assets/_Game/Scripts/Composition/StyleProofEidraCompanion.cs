using UnityEngine;

namespace Eidren.Composition
{
	public sealed class StyleProofEidraCompanion : MonoBehaviour
	{
		private PlayerPrefabBindings _player;

		private const int MaxLookupAttempts = 120;

		private int _lookupAttempts;

		private void Awake()
		{
			GameObject gameObject = Resources.Load<GameObject>("Prefabs/Actors/2D/Terrock_2D");
			if (gameObject == null)
			{
				Debug.LogWarning("[StyleProof] Terrock geometry is missing: Prefabs/Actors/2D/Terrock_2D");
				return;
			}
			GameObject gameObject2 = Object.Instantiate(gameObject, base.transform);
			gameObject2.name = "AuthoredTerrockVisual";
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
			gameObject2.transform.localScale = Vector3.one;
		}

		private void LateUpdate()
		{
			if (_player == null)
			{
				if (_lookupAttempts >= 120)
				{
					return;
				}
				_lookupAttempts++;
				_player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
				if (_player == null)
				{
					return;
				}
			}
			Transform transform = _player.transform;
			Vector3 b = transform.position - transform.right * 2.05f - transform.forward * 0.95f;
			base.transform.position = Vector3.Lerp(base.transform.position, b, 9f * Time.deltaTime);
		}
	}
}
