#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8618

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MagicBytes.services;

internal sealed class FileInfo (string fileExtension, string fileDescription, string fileClass, int headerOffset, byte [] headerHex, byte [] trailerHex)
{
	public string FileExtension { get; set; } = fileExtension;
	public string FileDescription { get; set; } = fileDescription;
	public string FileClass { get; set; } = fileClass;
	public int HeaderOffset { get; set; } = headerOffset;
	public byte [] HeaderHex { get; set; } = headerHex;
	public byte [] TrailerHex { get; set; } = trailerHex;
}

internal sealed class JsonService
{
	private sealed class FileDeserializer
	{
		[JsonPropertyName("File description")]
		public string FileDescriptionString { get; set; }

		[JsonPropertyName("Header (hex)")]
		public string HeaderHexString { get; set; }

		[JsonPropertyName("File extension")]
		public string FileExtensionString { get; set; }

		[JsonPropertyName("FileClass")]
		public string FileClassString { get; set; }

		[JsonPropertyName("Header offset")]
		public string HeaderOffsetString { get; set; }

		[JsonPropertyName("Trailer (hex)")]
		public string TrailerHexString { get; set; }
	}

	private readonly List<FileDeserializer>? _fileSigs;

	public JsonService ()
	{
		string jsonContent = GetJsonContent();
		_fileSigs = JsonSerializer.Deserialize<List<FileDeserializer>>(jsonContent);
	}

	internal List<FileInfo> GetFileInfoByExtension (string extension)
	{
		//List<FileDeserializer>? matchingFileSigs = _fileSigs?.Where(sig => sig.FileExtensionString.Split('|').Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase))).ToList();

		List<FileDeserializer> matchingFileSigs = [];

		if (_fileSigs != null)
		{
			foreach (FileDeserializer sig in _fileSigs)
			{
				string [] extensions = sig.FileExtensionString.Split('|');
				foreach (string ext in extensions)
				{
					if (ext.Equals(extension, StringComparison.OrdinalIgnoreCase))
					{
						matchingFileSigs.Add(sig);
						break;
					}
				}
			}
		}

		List<FileInfo> result = [];

		foreach (FileDeserializer fileSig in matchingFileSigs)
		{
			byte [] headerHex = ParseHexString(fileSig.HeaderHexString);
			byte [] trailerHex = fileSig.TrailerHexString is not "(null)" ? ParseHexString(fileSig.TrailerHexString) : [];
			int headerOffset = int.TryParse(fileSig.HeaderOffsetString, out int offset) ? offset : 0;
			string fileClass = fileSig.FileClassString;
			string fileDescription = fileSig.FileDescriptionString;
			string fileExtension = fileSig.FileExtensionString;

			result.Add(new FileInfo(fileExtension, fileDescription, fileClass, headerOffset, headerHex, trailerHex));
		}

		return result;
	}

	private static byte [] ParseHexString (string hexString)
	{
		// TODO: Правильно обрабатывать тот случай, когда у нас есть стрка, содержащая вопросительные знаки
		// такая есть только одна, "File description": "PKZIP archive_1"
		if (hexString.Contains('?', StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		hexString = hexString.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
		byte [] bytes = new byte [hexString.Length / 2];
		for (int i = 0; i < hexString.Length; i += 2)
		{
			bytes [i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
		}

		return bytes;
	}

	private static string GetJsonContent ()
	{
		string dbPath = "MagicBytes.resources.extensions.json";
		using StreamReader reader = new(Assembly.GetExecutingAssembly().GetManifestResourceStream(dbPath));
		return reader.ReadToEnd();
	}
}
