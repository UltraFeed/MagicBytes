namespace MagicBytes.services;

internal sealed class DirectoryService
{
	private readonly string dirPath = @"C:\Program Files (x86)\VMware\VMware Player";
	private readonly string [] files;

	public DirectoryService ()
	{
		files = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories);
	}

	public string [] GetFiles ()
	{
		return files;
	}
}
