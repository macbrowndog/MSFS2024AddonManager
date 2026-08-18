using System.Text.Json;

namespace MSFS2024AddonManager.Services;

internal sealed class AtomicJsonFileStore<T>
    where T : class
{
    private readonly JsonSerializerOptions jsonOptions;
    private readonly string path;

    public AtomicJsonFileStore(string path, JsonSerializerOptions jsonOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(jsonOptions);

        this.path = Path.GetFullPath(path);
        this.jsonOptions = jsonOptions;
    }

    public string BackupPath => $"{path}.bak";

    public T Load(Func<T> createDefault, Action<T>? normalize = null)
    {
        ArgumentNullException.ThrowIfNull(createDefault);

        if (!File.Exists(path))
        {
            if (!File.Exists(BackupPath))
            {
                T defaultValue = createDefault();
                normalize?.Invoke(defaultValue);
                return defaultValue;
            }

            T recovered = ReadBackupOrThrow(null);
            normalize?.Invoke(recovered);
            RestorePrimary(recovered);
            return recovered;
        }

        try
        {
            T value = ReadAndValidate(path);
            normalize?.Invoke(value);
            return value;
        }
        catch (JsonException primaryError)
        {
            T recovered = ReadBackupOrThrow(primaryError);
            normalize?.Invoke(recovered);
            RestorePrimary(recovered);
            return recovered;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw CreatePersistenceException($"Could not read '{path}'.", error);
        }
    }

    public void Save(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = CreateTemporaryPath();
        try
        {
            WriteAndValidateTemporaryFile(temporaryPath, value);

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, BackupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, path);
                CreateInitialBackup();
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            throw CreatePersistenceException($"Could not save '{path}' atomically.", error);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private void CreateInitialBackup()
    {
        string temporaryBackupPath = CreateTemporaryPath();
        try
        {
            File.Copy(path, temporaryBackupPath);
            _ = ReadAndValidate(temporaryBackupPath);
            File.Move(temporaryBackupPath, BackupPath, overwrite: true);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryBackupPath);
        }
    }

    private JsonPersistenceException CreatePersistenceException(string message, Exception error)
    {
        return new JsonPersistenceException(message, path, BackupPath, error);
    }

    private string CreateTemporaryPath()
    {
        string? directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        return Path.Combine(directory ?? string.Empty, $".{fileName}.{Guid.NewGuid():N}.tmp");
    }

    private T ReadAndValidate(string filePath)
    {
        using FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        return JsonSerializer.Deserialize<T>(stream, jsonOptions) ??
            throw new JsonException($"'{filePath}' contains a null JSON root.");
    }

    private T ReadBackupOrThrow(JsonException? primaryError)
    {
        if (!File.Exists(BackupPath))
        {
            throw CreatePersistenceException(
                $"'{path}' is invalid and no backup exists. The file was left unchanged.",
                primaryError ?? (Exception)new FileNotFoundException("The primary file is missing.", path));
        }

        try
        {
            return ReadAndValidate(BackupPath);
        }
        catch (Exception backupError) when (
            backupError is JsonException or IOException or UnauthorizedAccessException)
        {
            Exception combinedError = primaryError is null
                ? backupError
                : new AggregateException(primaryError, backupError);

            throw CreatePersistenceException(
                $"Neither '{path}' nor its backup is valid. Both files were left unchanged.",
                combinedError);
        }
    }

    private void RestorePrimary(T recoveredValue)
    {
        string temporaryPath = CreateTemporaryPath();
        try
        {
            WriteAndValidateTemporaryFile(temporaryPath, recoveredValue);

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            throw CreatePersistenceException(
                $"The backup is valid, but '{path}' could not be restored atomically.",
                error);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static void TryDeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (IOException)
        {
            // The committed data is valid; a locked temporary file can be cleaned up later.
        }
        catch (UnauthorizedAccessException)
        {
            // The committed data is valid; a locked temporary file can be cleaned up later.
        }
    }

    private void WriteAndValidateTemporaryFile(string temporaryPath, T value)
    {
        using (FileStream stream = new(
                   temporaryPath,
                   FileMode.CreateNew,
                   FileAccess.Write,
                   FileShare.None))
        {
            JsonSerializer.Serialize(stream, value, jsonOptions);
            stream.Flush(flushToDisk: true);
        }

        _ = ReadAndValidate(temporaryPath);
    }
}

internal sealed class JsonPersistenceException : IOException
{
    public JsonPersistenceException(
        string message,
        string primaryPath,
        string backupPath,
        Exception innerException)
        : base(message, innerException)
    {
        PrimaryPath = primaryPath;
        BackupPath = backupPath;
    }

    public string PrimaryPath { get; }

    public string BackupPath { get; }
}
