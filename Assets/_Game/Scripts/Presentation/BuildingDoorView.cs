using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// F31-008: Die Tür öffnet automatisch, wenn die Spielfigur in den Sensor
	/// tritt, und schließt nach dem Verlassen. Das Blatt schwenkt um die
	/// Angel; solange die Tür nicht vollständig geschlossen ist, ist der
	/// Sperr-Collider aus. Kein Spielstandfeld: Nach dem Laden ist jede Tür
	/// geschlossen (§17 bleibt unberührt). Der NavMeshObstacle bleibt wie
	/// gehabt deaktiviert — die Wegfindung behandelt die Tür schon heute als
	/// offen, nur die Physik sperrte den Spieler aus.
	/// Sitzt auf dem Sensor-Kind (Trigger), damit OnTrigger* ohne Rigidbody
	/// am Wurzelobjekt ankommt.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class BuildingDoorView : MonoBehaviour
	{
		[SerializeField]
		private Transform blade;

		[SerializeField]
		private Collider blockingCollider;

		[SerializeField]
		private float openYaw = 105f;

		[SerializeField]
		private float degreesPerSecond = 260f;

		private int _occupants;

		private float _currentYaw;

		public bool IsOpen => _occupants > 0;

		public float CurrentYaw => _currentYaw;

		public Transform Blade => blade;

		public Collider BlockingCollider => blockingCollider;

		public void ConfigureReferences(Transform configuredBlade, Collider configuredBlockingCollider)
		{
			blade = configuredBlade;
			blockingCollider = configuredBlockingCollider;
		}

		private void Update()
		{
			float target = IsOpen ? openYaw : 0f;
			if (!Mathf.Approximately(_currentYaw, target))
			{
				_currentYaw = Mathf.MoveTowards(_currentYaw, target, degreesPerSecond * Time.deltaTime);
				if (blade != null)
				{
					blade.localRotation = Quaternion.Euler(0f, _currentYaw, 0f);
				}
			}
			if (blockingCollider != null)
			{
				bool geschlossen = !IsOpen && _currentYaw < 1f;
				if (blockingCollider.enabled != geschlossen)
				{
					blockingCollider.enabled = geschlossen;
				}
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (IsPlayer(other))
			{
				_occupants++;
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (IsPlayer(other))
			{
				_occupants = Mathf.Max(0, _occupants - 1);
			}
		}

		private void OnDisable()
		{
			_occupants = 0;
		}

		private static bool IsPlayer(Collider other)
		{
			return other != null && other.GetComponent<CharacterController>() != null;
		}
	}
}
