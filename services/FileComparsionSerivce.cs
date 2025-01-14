#pragma warning disable CA1305

using System.Text;

namespace MagicBytes.services;

// TODO: Анатолий поменял метод GetFileInfoByExtension, поэтому нужно учитывать что будет приходить несколько кортежей с данными о файле по отдавемому расширению
// нужно поменять логику с одного кортежа на список кортежей и проверять на то, что пришел один кортеж, а если пришел не один не отправлять нахуй сразу,
// а прописать доп логику проверок и только если везде хуйня, слать нахуй

// Тут мы должны посмотреть все наши файлики в папке: по очереди берем расширение каждого файлика и обращаемся к методу
// из JsonService: который возвращает который возвращает инфу по расширению файла.
// Мы читаем байты каждого нашего файлика и сравниваем сигнатуру из нашего файлика с сигнатурой из signs.
// Если какая-то хуйня, мы должны написать какое расширение ожидалось на самом деле.
// если такое расширение не найдено, то говорим что в базе такого нет

internal static class FileComparsionService
{
	public static string CompareExtensions ()
	{
		JsonService jsonService = new();
		DirectoryService directoryService = new();
		StringBuilder result = new();

		string [] allFilesPathes = directoryService.GetFiles();

		foreach (string filePath in allFilesPathes)
		{
			string extension = Path.GetExtension(filePath).Replace(".", "", StringComparison.OrdinalIgnoreCase);
			List<FileInfo> listOfFileInfo = jsonService.GetFileInfoByExtension(extension);

			// TODO: Eсли listOfFileInfo.Count == 0, то говорим что расширения в базе нет (вроде логично, но я не уверен в этих миллиардах циклов)
			// Ты проверь, правильно ли я понял все
			if (listOfFileInfo.Count == 0)
			{
				//_ = result.AppendLine($"Расширение файла {filePath} отсутствет в базе");
				continue;
			}

			bool foundSignature = false;
			List<string> matchedExtensions = []; // тут по идее храним список совпавших расширений, чтобы дать совет юзеру
			foreach (FileInfo fileInfo in listOfFileInfo)
			{
				matchedExtensions.Add(fileInfo.FileExtension);
				// TODO: Тогда зачем эта проверка тут?????
				if (IsEmptyFileInfo(fileInfo))
				{
					continue;
				}

				if (CheckFileSignature(filePath, fileInfo))
				{
					foundSignature = true;
					break;
				}
			}

			if (foundSignature)
			{
				//_ = result.AppendLine($"{Path.GetFileName(filePath)} имеет правильное расширение");
			}
			else
			{
				_ = result.AppendLine($"{Path.GetFileName(filePath)} имеет неверное расширение. Ожидаемые расширения: {string.Join(", ", matchedExtensions)}");
			}
		}

		return result.ToString();
	}

	private static bool IsEmptyFileInfo (FileInfo fileInfo)
	{
		return fileInfo.HeaderHex == null && fileInfo.TrailerHex == null && fileInfo.HeaderOffset == 0 && fileInfo.FileClass == null && fileInfo.FileDescription == null && fileInfo.FileExtension == null;
	}

	private static bool CheckFileSignature (string filePath, FileInfo fileInfo)
	{
		if (fileInfo.HeaderHex != null && fileInfo.HeaderHex.Length > 0)
		{
			int headerBytesToRead = fileInfo.HeaderHex.Length + fileInfo.HeaderOffset;
			byte [] headerBuffer = ReadFileBytes(filePath, headerBytesToRead);

			if (!StartsWith(headerBuffer, fileInfo.HeaderHex, fileInfo.HeaderOffset))
			{
				return false;
			}
		}

		if (fileInfo.TrailerHex != null && fileInfo.TrailerHex.Length > 0)
		{
			byte [] trailerBuffer = ReadLastFileBytes(filePath, fileInfo.TrailerHex.Length);

			if (!EndsWith(trailerBuffer, fileInfo.TrailerHex))
			{
				return false;
			}
		}

		return true;
	}

	private static bool StartsWith (byte [] fileBytes, byte [] headerHex, int headerOffset)
	{
		if (fileBytes.Length < headerOffset + headerHex.Length)
		{
			return false;
		}

		for (int i = 0; i < headerHex.Length; i++)
		{
			if (fileBytes [headerOffset + i] != headerHex [i])
			{
				return false;
			}
		}

		return true;
	}

	private static bool EndsWith (byte [] fileBytes, byte [] trailerHex)
	{
		if (fileBytes.Length < trailerHex.Length)
		{
			return false;
		}

		for (int i = 0; i < trailerHex.Length; i++)
		{
			if (fileBytes [fileBytes.Length - trailerHex.Length + i] != trailerHex [i])
			{
				return false;
			}
		}

		return true;
	}

	private static byte [] ReadFileBytes (string filePath, int bytesToRead)
	{
		using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
		byte [] buffer = new byte [bytesToRead];
		_ = fileStream.Read(buffer, 0, bytesToRead);
		return buffer;
	}

	private static byte [] ReadLastFileBytes (string filePath, int bytesToRead)
	{
		using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
		byte [] buffer = new byte [bytesToRead];
		_ = fileStream.Seek(-bytesToRead, SeekOrigin.End);
		_ = fileStream.Read(buffer, 0, bytesToRead);
		return buffer;
	}
}
