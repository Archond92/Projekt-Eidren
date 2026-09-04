using System.IO;

namespace Eidren.Core.Services
{
	public sealed class PhysicalSaveFileSystem : ISaveFileSystem
	{
		public bool FileExists(string path)
		{
			return File.Exists(path);
		}

		public string ReadAllText(string path)
		{
			return File.ReadAllText(path);
		}

		public void WriteAllText(string path, string contents)
		{
			File.WriteAllText(path, contents);
		}

		public void Copy(string source, string destination, bool overwrite)
		{
			File.Copy(source, destination, overwrite);
		}

		public void Move(string source, string destination)
		{
			File.Move(source, destination);
		}

		public void Replace(string source, string destination)
		{
			File.Replace(source, destination, null);
		}

		public void Delete(string path)
		{
			File.Delete(path);
		}

		public void CreateDirectory(string path)
		{
			Directory.CreateDirectory(path);
		}
	}
}
