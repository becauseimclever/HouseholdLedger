// <copyright file="ComposedClientStaticFiles.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

using System.Security.Cryptography;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

internal sealed class ComposedClientStaticFiles
{
    private const string ClientManifestFileName = "household-ledger-client.manifest";
    private const string CompositionManifestFileName = "household-ledger-client.composition.manifest";
    private const string EntryDocumentName = "index.html";

    private ComposedClientStaticFiles(IFileProvider fileProvider, string entryDocumentPath)
    {
        this.FileProvider = fileProvider;
        this.EntryDocumentPath = entryDocumentPath;
    }

    public IFileProvider FileProvider { get; }

    public string EntryDocumentPath { get; }

    public static ComposedClientStaticFiles? TryCreate(string packageRoot)
    {
        var compositionManifestPath = Path.Combine(packageRoot, CompositionManifestFileName);
        var clientManifestPath = Path.Combine(packageRoot, ClientManifestFileName);
        if (!File.Exists(compositionManifestPath) || !File.Exists(clientManifestPath))
        {
            return null;
        }

        try
        {
            var compositionInventory = ReadCompositionManifest(compositionManifestPath, clientManifestPath);
            var clientInventory = ReadClientManifest(clientManifestPath);
            if (!compositionInventory.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .SequenceEqual(clientInventory.OrderBy(pair => pair.Key, StringComparer.Ordinal)))
            {
                return null;
            }

            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (relativePath, expectedHash) in compositionInventory)
            {
                var physicalPath = GetChildPath(packageRoot, relativePath);
                if (!File.Exists(physicalPath) || !HashMatches(physicalPath, expectedHash))
                {
                    return null;
                }

                files.Add(relativePath, physicalPath);
            }

            if (!files.TryGetValue(EntryDocumentName, out var entryDocumentPath))
            {
                return null;
            }

            return new ComposedClientStaticFiles(new InventoryFileProvider(files), entryDocumentPath);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static Dictionary<string, string> ReadCompositionManifest(string compositionManifestPath, string clientManifestPath)
    {
        var lines = File.ReadAllLines(compositionManifestPath);
        var expectedHeader = new[]
        {
            "composition-contract-version=1",
            "composition-command-version=1",
            "client-artifact-identity=household-ledger-client@1.0.0",
            "client-contract-version=1",
            $"client-manifest-path={ClientManifestFileName}",
            $"client-manifest-sha256={GetSha256(clientManifestPath)}",
            "client-content-root=wwwroot",
            $"client-entry-point={EntryDocumentName}",
            "destination=/",
            "file-sha256",
        };

        return ReadInventory(lines, expectedHeader);
    }

    private static Dictionary<string, string> ReadClientManifest(string clientManifestPath)
    {
        var lines = File.ReadAllLines(clientManifestPath);
        var expectedHeader = new[]
        {
            "contract-version=1",
            "artifact-identity=household-ledger-client@1.0.0",
            "content-root=wwwroot",
            $"entry-point={EntryDocumentName}",
            "file-sha256",
        };

        return ReadInventory(lines, expectedHeader);
    }

    private static Dictionary<string, string> ReadInventory(string[] lines, string[] expectedHeader)
    {
        if (lines.Length <= expectedHeader.Length || !lines.Take(expectedHeader.Length).SequenceEqual(expectedHeader, StringComparer.Ordinal))
        {
            throw new InvalidDataException("The Client artifact manifest header is invalid.");
        }

        var inventory = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in lines.Skip(expectedHeader.Length))
        {
            var parts = line.Split('|');
            if (parts.Length != 2 || !IsSha256(parts[1]) || !inventory.TryAdd(NormalizeRelativePath(parts[0]), parts[1]))
            {
                throw new InvalidDataException("The Client artifact manifest inventory is invalid.");
            }
        }

        return inventory;
    }

    private static string NormalizeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.Contains('\\'))
        {
            throw new InvalidDataException("The Client artifact manifest path is invalid.");
        }

        var segments = path.Split('/');
        if (segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment is "." or ".."))
        {
            throw new InvalidDataException("The Client artifact manifest path is invalid.");
        }

        return path;
    }

    private static string GetChildPath(string packageRoot, string relativePath)
    {
        var root = Path.GetFullPath(packageRoot);
        var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The Client artifact path escapes the package root.");
        }

        return candidate;
    }

    private static bool HashMatches(string path, string expectedHash)
    {
        return string.Equals(GetSha256(path), expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSha256(string path)
    {
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    }

    private static bool IsSha256(string value)
    {
        return value.Length == 64 && value.All(Uri.IsHexDigit);
    }

    private sealed class InventoryFileProvider(IReadOnlyDictionary<string, string> files) : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return NotFoundDirectoryContents.Singleton;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return files.TryGetValue(subpath.TrimStart('/'), out var path)
                ? new InventoryFileInfo(path)
                : new NotFoundFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }
    }

    private sealed class InventoryFileInfo(string path) : IFileInfo
    {
        private readonly FileInfo file = new(path);

        public bool Exists => this.file.Exists;

        public long Length => this.file.Length;

        public string? PhysicalPath => this.file.FullName;

        public string Name => this.file.Name;

        public DateTimeOffset LastModified => this.file.LastWriteTimeUtc;

        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            return this.file.OpenRead();
        }
    }
}
