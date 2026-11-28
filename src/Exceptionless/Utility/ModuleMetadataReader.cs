using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Exceptionless.Extensions;
using Exceptionless.Logging;

namespace Exceptionless.Utility {
    internal sealed class ModuleMetadataReader {
        private static readonly string[] _msPublicKeyTokens = {
            "b77a5c561934e089",
            "b03f5f7f11d50a3a",
            "31bf3856ad364e35"
        };
        private readonly ConcurrentDictionary<string, ModuleMetadata> _cache = new();
        private readonly Queue<string> _paths = new();

        public ModuleMetadataReader(int cacheCapacity) {
            if (cacheCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(cacheCapacity));
            CacheCapacity = cacheCapacity;
        }

        public int CacheCapacity { get; }

        public ModuleMetadata Read(Assembly assembly, string path, bool includeSystem, bool allowCacheEviction, IExceptionlessLog log) {
            Guid versionId = Guid.Empty;
            if (!String.IsNullOrEmpty(path)) {
                try {
                    versionId = assembly.ManifestModule.ModuleVersionId;
                } catch {
                    // Restricted reflection can still provide the usual module metadata.
                }
            }

            ModuleMetadata metadata = null;
            if (versionId != Guid.Empty && _cache.TryGetValue(path, out var cached) && cached.VersionId == versionId)
                metadata = cached;

            AssemblyName name = null;
            string publicKeyToken = metadata?.PublicKeyToken;
            if (metadata is null) {
                name = assembly.GetAssemblyName();
                if (name?.GetPublicKeyToken() is { Length: > 0 } token)
                    publicKeyToken = token.ToHex();
            }

            if (!includeSystem) {
                try {
                    if (_msPublicKeyTokens.Contains(publicKeyToken) || assembly.IsDefined(typeof(GeneratedCodeAttribute), inherit: false))
                        return null;
                } catch (Exception ex) {
                    log?.Error(typeof(ExceptionlessClient), ex, "Error while checking if assembly " + assembly.FullName + " should be added to modules:" + ex.Message);
                }
            }

            if (metadata is not null)
                return metadata;

            metadata = new ModuleMetadata(assembly, name, publicKeyToken, path, versionId);
            if (versionId != Guid.Empty)
                Store(path, metadata, allowCacheEviction);
            return metadata;
        }

        private void Store(string path, ModuleMetadata metadata, bool allowEviction) {
            lock (_paths) {
                if (!_cache.ContainsKey(path)) {
                    if (_paths.Count == CacheCapacity) {
                        if (!allowEviction)
                            return;

                        _cache.TryRemove(_paths.Dequeue(), out _);
                    }

                    _paths.Enqueue(path);
                }

                _cache[path] = metadata;
            }
        }
    }
}
