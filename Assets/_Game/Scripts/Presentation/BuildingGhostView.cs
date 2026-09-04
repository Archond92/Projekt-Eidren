using System;
using UnityEngine.AI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Presentation
{
	public sealed class BuildingGhostView : MonoBehaviour
	{
		public const string ResourceFolder = "Art/Buildings";

		// F31-017: Bodenteile reichen bis y = 0,14 — mit 0,07 lag die Marke
		// unter der Bodenoberkante und war auf gelegten Boeden unsichtbar.
		private const float MarkHeight = 0.2f;

		/// <summary>
		/// Lokale Lage der Vorschaumarke. Muss ueber der Oberkante des
		/// hoechsten begehbaren Bauteils liegen (Boden: 0,14), sonst
		/// verdeckt ein gelegter Boden die gruene Umrandung (F31-017).
		/// </summary>
		public static Vector3 MarkLocalPosition => new Vector3(0f, MarkHeight, 0f);

		private GameObject _visual;

		private Renderer[] _renderers = Array.Empty<Renderer>();

		private Material[] _tints;

		private Material[] _markStyles;

		private BuildGridMark _mark;

		private BuildingPreviewSignal _signal;

		private bool _signalApplied;

		private void Awake()
		{
			_tints = new Material[3]
			{
				Load("BLD_Preview_Valid"),
				Load("BLD_Preview_Conditional"),
				Load("BLD_Preview_Invalid")
			};
			_markStyles = new Material[3]
			{
				Load("BLD_PreviewMark_Valid"),
				Load("BLD_PreviewMark_Conditional"),
				Load("BLD_PreviewMark_Invalid")
			};
			GameObject gameObject = Resources.Load<GameObject>("Art/Buildings/BLD_Preview_Mark");
			if (gameObject == null)
			{
				throw new InvalidOperationException("Vorschaumarke fehlt: Art/Buildings/BLD_Preview_Mark");
			}
			_mark = UnityEngine.Object.Instantiate(gameObject, base.transform, worldPositionStays: false).GetComponent<BuildGridMark>();
			_mark.transform.localPosition = MarkLocalPosition;
		}

		public void Show(GameObject prefab, Vector3 position, float rotationDegrees, Vector2 footprint)
		{
			if (!(prefab == null))
			{
				if (_visual == null || _visual.name != prefab.name)
				{
					Rebuild(prefab);
				}
				base.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotationDegrees, 0f));
				_mark.transform.localScale = new Vector3(footprint.x, 1f, footprint.y);
				base.gameObject.SetActive(value: true);
			}
		}

		public void SetState(BuildingPreviewSignal signal)
		{
			if (_signalApplied && signal == _signal)
			{
				return;
			}
			_signal = signal;
			_signalApplied = true;
			Material material = _tints[(int)signal];
			Renderer[] renderers = _renderers;
			foreach (Renderer renderer in renderers)
			{
				int num = Mathf.Max(1, renderer.sharedMaterials.Length);
				Material[] array = new Material[num];
				for (int j = 0; j < num; j++)
				{
					array[j] = material;
				}
				renderer.sharedMaterials = array;
			}
			_mark.gameObject.SetActive(value: true);
			_mark.SetMaterial(_markStyles[(int)signal]);
		}

		public void Hide()
		{
			Clear();
			if (_mark != null)
			{
				_mark.gameObject.SetActive(value: false);
			}
			base.gameObject.SetActive(value: false);
		}

		private void Rebuild(GameObject prefab)
		{
			Clear();
			_visual = UnityEngine.Object.Instantiate(prefab, base.transform);
			_visual.name = prefab.name;
			Collider[] componentsInChildren = _visual.GetComponentsInChildren<Collider>(includeInactive: true);
			foreach (Collider collider in componentsInChildren)
			{
				collider.enabled = false;
			}
			NavMeshObstacle[] componentsInChildren2 = _visual.GetComponentsInChildren<NavMeshObstacle>(includeInactive: true);
			foreach (NavMeshObstacle navMeshObstacle in componentsInChildren2)
			{
				navMeshObstacle.enabled = false;
			}
			MonoBehaviour[] componentsInChildren3 = _visual.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
			foreach (MonoBehaviour monoBehaviour in componentsInChildren3)
			{
				monoBehaviour.enabled = false;
			}
			_renderers = _visual.GetComponentsInChildren<Renderer>(includeInactive: true);
		}

		private void Clear()
		{
			if (_visual != null)
			{
				UnityEngine.Object.Destroy(_visual);
			}
			_visual = null;
			_renderers = Array.Empty<Renderer>();
			_signalApplied = false;
		}

		private static Material Load(string assetName)
		{
			Material material = Resources.Load<Material>("Art/Buildings/" + assetName);
			if (!(material != null))
			{
				throw new InvalidOperationException("Vorschaumaterial fehlt: Art/Buildings/" + assetName);
			}
			return material;
		}
	}
}
