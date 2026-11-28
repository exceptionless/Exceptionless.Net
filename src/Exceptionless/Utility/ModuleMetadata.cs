using System;
using System.IO;
using System.Reflection;
using Module = Exceptionless.Models.Data.Module;

namespace Exceptionless.Utility {
    // The cache can outlive an assembly's load context. Keep only strings and values here.
    internal sealed class ModuleMetadata {
        private readonly string _name;
        private readonly string _version;
        private readonly string _assemblyVersion;
        private readonly string _productVersion;
        private readonly string _fileVersion;
        private readonly DateTime _createdDate;
        private readonly DateTime _modifiedDate;

        public ModuleMetadata(Assembly assembly, AssemblyName name, string publicKeyToken, string path, Guid versionId) {
            VersionId = versionId;
            PublicKeyToken = publicKeyToken;
            _name = name?.Name;

            string assemblyVersion = name?.Version?.ToString();
            string infoVersion = assembly.GetInformationalVersion();
            string fileVersion = assembly.GetFileVersion();
            if (name is not null)
                _version = infoVersion ?? fileVersion ?? assemblyVersion;

            // Module.Data carries only versions that differ from the displayed version.
            if (!String.IsNullOrEmpty(assemblyVersion) && assemblyVersion != _version)
                _assemblyVersion = assemblyVersion;
            if (!String.IsNullOrEmpty(infoVersion) && infoVersion != _version)
                _productVersion = infoVersion;
            if (!String.IsNullOrEmpty(fileVersion) && fileVersion != _version)
                _fileVersion = fileVersion;

            if (!String.IsNullOrEmpty(path)) {
                try {
                    var file = new FileInfo(path);
                    _createdDate = file.CreationTimeUtc;
                    _modifiedDate = file.LastWriteTimeUtc;
                    return;
                } catch {
                    // Try each timestamp independently on restricted or unavailable files.
                }
            }

            _createdDate = assembly.GetCreationTime().GetValueOrDefault();
            _modifiedDate = assembly.GetLastWriteTime().GetValueOrDefault();
        }

        public Guid VersionId { get; }
        public string PublicKeyToken { get; }

        public Module CreateModule(bool isEntry) {
            var module = new Module {
                Name = _name,
                Version = _version,
                IsEntry = isEntry,
                CreatedDate = _createdDate,
                ModifiedDate = _modifiedDate
            };
            if (PublicKeyToken is not null)
                module.Data["PublicKeyToken"] = PublicKeyToken;
            if (_assemblyVersion is not null)
                module.Data["Version"] = _assemblyVersion;
            if (_productVersion is not null)
                module.Data["ProductVersion"] = _productVersion;
            if (_fileVersion is not null)
                module.Data["FileVersion"] = _fileVersion;
            return module;
        }
    }
}
