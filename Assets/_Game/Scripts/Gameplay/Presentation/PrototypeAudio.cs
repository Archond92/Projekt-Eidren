using Eidren.Core.Services;
using System;
using UnityEngine;

namespace Eidren.Gameplay.Presentation
{
	public sealed class PrototypeAudio : MonoBehaviour
	{
		public AudioService Service { get; private set; }

		public void Initialize(AudioService service)
		{
			Service = service ?? throw new ArgumentNullException("service");
		}
	}
}
