using MagicBytes.services;

internal static class Program
{
	private static void Main ()
	{
		Console.WriteLine(FileComparsionService.CompareExtensions());
		_ = Console.ReadLine();
	}
}
