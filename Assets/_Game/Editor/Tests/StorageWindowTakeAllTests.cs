using Eidren.Interaction;
using Eidren.UI;
using NUnit.Framework;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class StorageWindowTakeAllTests
	{
		[Test]
		public void AllesNehmen_GiltFuerBeutequellenNichtFuerHeimatlager()
		{
			GameObject host = new GameObject("TakeAllRule_Test");
			try
			{
				Assert.That(StorageWindow.AllowsTakeAll(host.AddComponent<WorldChestContainer>()), Is.True, "Gebietskisten behalten ALLES NEHMEN");
				Assert.That(StorageWindow.AllowsTakeAll(host.AddComponent<EnemyLootContainer>()), Is.True, "Gegnerloot muss ALLES NEHMEN anbieten");
				Assert.That(StorageWindow.AllowsTakeAll(host.AddComponent<StorageContainer>()), Is.False, "Heimatlager erhalten die Einwegaktion nicht automatisch");
				Assert.That(StorageWindow.AllowsTakeAll(null), Is.False, "Ohne Behälter keine Sammelentnahme");
			}
			finally
			{
				Object.DestroyImmediate(host);
			}
		}
	}
}
