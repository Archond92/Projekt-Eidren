namespace Eidren.Core.Services
{
	public interface ISaveFileSystem
	{
		bool FileExists(string path);

		string ReadAllText(string path);

		void WriteAllText(string path, string contents);

		void Copy(string source, string destination, bool overwrite);

		void Move(string source, string destination);

		void Replace(string source, string destination);

		void Delete(string path);

		void CreateDirectory(string path);
	}
}
