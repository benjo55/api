using api.Interfaces;

namespace api.Services.LegalDocuments
{
    public sealed class DocumentBinaryStorage : IDocumentBinaryStorage
    {
        private readonly string _rootPath;

        public DocumentBinaryStorage(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _rootPath = configuration["DocumentStorage:RootPath"]
                ?? Path.Combine(environment.ContentRootPath, "App_Data", "document-artifacts");
        }

        public async Task<(string StorageKey, string Hash, long Size)> SaveAsync(byte[] content, string extension, CancellationToken cancellationToken = default)
        {
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
            var normalizedExtension = extension.StartsWith('.') ? extension : $".{extension}";
            var folder = Path.Combine(hash[..2], hash[2..4]);
            var fileName = $"{hash}{normalizedExtension}";
            var relativePath = Path.Combine(folder, fileName);
            var fullPath = Path.Combine(_rootPath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            if (!File.Exists(fullPath))
            {
                await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
            }

            return (relativePath.Replace('\\', '/'), hash, content.LongLength);
        }

        public async Task<byte[]> ReadAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedKey));
            var root = Path.GetFullPath(_rootPath);
            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid storage key.");
            }

            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }
    }
}
