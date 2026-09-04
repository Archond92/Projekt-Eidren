using Eidren.Core.Services;
using UnityEngine;

namespace Eidren.Composition
{
	[RequireComponent(typeof(BoxCollider))]
	public sealed class MapExitVolume : MonoBehaviour
	{
		[SerializeField]
		private string stableExitId;

		[SerializeField]
		private MapExitDirection direction;

		[SerializeField]
		private string zoneId;

		[SerializeField]
		private bool exitEnabled = true;

		[SerializeField]
		private float transitionDuration = 0.4f;

		[SerializeField]
		private string targetScene = "WorldMap";

		[SerializeField]
		private string targetNodeId;

		[SerializeField]
		private MapExitCoordinator coordinator;

		public string StableExitId => stableExitId;

		public MapExitDirection Direction => direction;

		public string ZoneId => zoneId;

		public bool ExitEnabled => exitEnabled;

		public float TransitionDuration => Mathf.Max(0.01f, transitionDuration);

		public string TargetScene => targetScene;

		public string TargetNodeId => targetNodeId;

		public Bounds TriggerBounds
		{
			get
			{
				BoxCollider component = GetComponent<BoxCollider>();
				return (component != null) ? component.bounds : new Bounds(base.transform.position, Vector3.zero);
			}
		}

		public void Configure(string configuredExitId, MapExitDirection configuredDirection, string configuredZoneId, bool active, float duration, string configuredTargetScene, string configuredTargetNodeId, MapExitCoordinator exitCoordinator)
		{
			stableExitId = configuredExitId;
			direction = configuredDirection;
			zoneId = configuredZoneId;
			exitEnabled = active;
			transitionDuration = Mathf.Max(0.01f, duration);
			targetScene = configuredTargetScene;
			targetNodeId = configuredTargetNodeId;
			coordinator = exitCoordinator;
			BoxCollider component = GetComponent<BoxCollider>();
			component.isTrigger = true;
		}

		public void SetExitEnabled(bool active)
		{
			if (exitEnabled != active)
			{
				exitEnabled = active;
				if (!active)
				{
					coordinator?.NotifyVolumeDisabled(this);
				}
			}
		}

		private void Awake()
		{
			if ((object)coordinator == null)
			{
				coordinator = GetComponentInParent<MapExitCoordinator>();
			}
			BoxCollider component = GetComponent<BoxCollider>();
			component.isTrigger = true;
			if (string.IsNullOrWhiteSpace(stableExitId))
			{
				Debug.LogError("MapExitVolume '" + base.name + "' requires a stable exit ID.", this);
				exitEnabled = false;
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (exitEnabled && !(coordinator == null))
			{
				PlayerPrefabBindings componentInParent = other.GetComponentInParent<PlayerPrefabBindings>();
				if (componentInParent != null)
				{
					coordinator.NotifyEntered(this, componentInParent);
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (!(coordinator == null))
			{
				PlayerPrefabBindings componentInParent = other.GetComponentInParent<PlayerPrefabBindings>();
				if (componentInParent != null)
				{
					coordinator.NotifyExited(this, componentInParent);
				}
			}
		}

		private void OnDisable()
		{
			coordinator?.NotifyVolumeDisabled(this);
		}
	}
}
