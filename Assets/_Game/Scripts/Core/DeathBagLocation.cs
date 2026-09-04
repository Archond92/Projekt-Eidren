using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public readonly struct DeathBagLocation
	{
		public const string ContainerId = "death.bag";

		public string SceneKey { get; }

		public Vector3 Position { get; }

		public bool Exists => !string.IsNullOrWhiteSpace(SceneKey);

		public static DeathBagLocation None => new DeathBagLocation(string.Empty, Vector3.zero);

		public DeathBagLocation(string sceneKey, Vector3 position)
		{
			SceneKey = sceneKey ?? string.Empty;
			Position = position;
		}

		public bool LiesIn(string sceneKey)
		{
			return Exists && string.Equals(SceneKey, sceneKey, StringComparison.Ordinal);
		}
	}
}
