using Microsoft.CodeAnalysis;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ScriptSourceGenerator;

public partial class CsxSourceGenerator
{
    private class NugetPackageReferenceResolver : MetadataReferenceResolver
    {
        private readonly ISettings _nugetSettings;
        private readonly MetadataReferenceResolver _defaultReferenceResolver;
        private readonly IDictionary<string, MetadataReference> _dependenciesOutput;
        private readonly string _globalPackagesFolder;
        private readonly PackagePathResolver _packagePathResolver;
        private readonly NuGetFramework _nugetFramework;
        private readonly FrameworkReducer _frameworkReducer;

        public NugetPackageReferenceResolver(MetadataReferenceResolver defaultReferenceResolver, IDictionary<string, MetadataReference> dependenciesOutput)
        {
            _nugetSettings = Settings.LoadDefaultSettings(null);
            _globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(_nugetSettings);
            _packagePathResolver = new PackagePathResolver(_globalPackagesFolder);
            _nugetFramework = NuGetFramework.ParseFolder("netstandard2.0");
            _frameworkReducer = new FrameworkReducer();
            _defaultReferenceResolver = defaultReferenceResolver;
            _dependenciesOutput = dependenciesOutput;
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return _defaultReferenceResolver.Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _defaultReferenceResolver.GetHashCode();
        }

        public override bool ResolveMissingAssemblies => _defaultReferenceResolver.ResolveMissingAssemblies;

        public override PortableExecutableReference? ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            return _defaultReferenceResolver.ResolveMissingAssembly(definition, referenceIdentity);
        }

        private static readonly Regex _nugetRefMatch = new(@"nuget[:]\s*(?<packageid>.*)[,/](?<version>.*)", RegexOptions.Compiled);
        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties)
        {
            var match = _nugetRefMatch.Match(reference);
            if (match.Success)
            {
                var packageId = match.Groups["packageid"].Value;
                var version = new NuGetVersion(match.Groups["version"].Value);
                var packageIdentity = new PackageIdentity(packageId, version);
                var nugetReferences = GetNugetReferences(packageIdentity);
                Debug.WriteLine(string.Join("\n", nugetReferences));

                foreach (var dependency in nugetReferences.Distinct()
                    .Skip(1)
                    .Select(i => MetadataReference.CreateFromFile(i)))
                {
                    _dependenciesOutput[dependency.FilePath!] = dependency;
                }

                return nugetReferences.Distinct()
                    .Take(1) //https://github.com/dotnet/roslyn/blob/6392f2f2d3106ac250e7dcc3b9a54478330add09/src/Compilers/Core/Portable/ReferenceManager/CommonReferenceManager.Resolution.cs#L885
                    .Select(i => MetadataReference.CreateFromFile(i))
                    .ToImmutableArray();
            }
            return _defaultReferenceResolver.ResolveReference(reference, baseFilePath, properties);
        }

        private IEnumerable<string> GetNugetReferences(PackageIdentity packageIdentity)
        {
            var package = GlobalPackagesFolderUtility.GetPackage(packageIdentity, _globalPackagesFolder);
            try
            {
                if (package == null)
                {
                    RestorePackageAsync(packageIdentity).Wait(); //TODO: If Roslyn supports resolving reference async, remove Wait and await.
                    package = GlobalPackagesFolderUtility.GetPackage(packageIdentity, _globalPackagesFolder);

                }
                if (package == null)
                {
                    throw new PackageNotFoundProtocolException(packageIdentity);
                }

                //dependencies
                var dependencies = package.PackageReader.GetPackageDependencies().SelectMany(dg => dg.Packages.SelectMany(dep => GetNugetReferences(new PackageIdentity(dep.Id, dep.VersionRange.MinVersion))));

                var frameworkItems = package.PackageReader.GetReferenceItems();
                var nearest = _frameworkReducer.GetNearest(_nugetFramework, frameworkItems.Select(x => x.TargetFramework));
                var installPath = _packagePathResolver.GetInstalledPath(packageIdentity) ?? Path.Combine(_globalPackagesFolder, packageIdentity.Id.ToLowerInvariant(), packageIdentity.Version.ToString());
                return frameworkItems
                        .Where(x => x.TargetFramework.Equals(nearest))
                        .SelectMany(x => x.Items.Select(i => Path.Combine(installPath, i)))
                        .Union(dependencies);
            }
            finally
            {
                package.Dispose();
            }            
        }

        private async Task RestorePackageAsync(PackageIdentity packageId)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            const string source = "https://api.nuget.org/v3/index.json";

            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(source);
            var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            // Download the package
            using var packageStream = new MemoryStream();
            await resource.CopyNupkgToStreamAsync(
                packageId.Id,
                packageId.Version,
                packageStream,
                cache,
                logger,
                cancellationToken);

            packageStream.Seek(0, SeekOrigin.Begin);

            // Add it to the global package folder
            using var downloadResult = await GlobalPackagesFolderUtility.AddPackageAsync(
                source,
                packageId,
                packageStream,
                _globalPackagesFolder,
                parentId: Guid.Empty,
                ClientPolicyContext.GetClientPolicy(_nugetSettings, logger),
                logger,
                cancellationToken);
            Debug.WriteLine($"Installation package {packageId} finished with Result: {downloadResult.Status}");
        }
    }
}
