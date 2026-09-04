using System.IO;

namespace Eidren.Editor
{
	/// <summary>
	/// Keeps completed Mid-Poly migrations idempotent after their legacy backup
	/// has been moved out of the Unity runtime tree into the documentation archive.
	/// </summary>
	internal static class MidpolyLegacyArchiveGuard
	{
		private const string ArchiveRoot = "Documentation/Etappen/MidPoly/LegacyArchive/";

		public static string ArchivePath(string runtimePath) => ArchiveRoot + runtimePath;

		public static bool IsArchived(string runtimePath) => File.Exists(ArchivePath(runtimePath));

		public static bool HasBackup(string runtimePath) => File.Exists(runtimePath) || IsArchived(runtimePath);
	}
}
