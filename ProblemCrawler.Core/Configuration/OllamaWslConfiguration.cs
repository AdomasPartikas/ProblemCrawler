using Microsoft.Extensions.Configuration;
namespace ProblemCrawler.Core.Configuration
{
    public sealed record OllamaWslConfiguration(
    IReadOnlyList<string> Models,
    string OllamaPath,
    string Password,
    string BaseUrl)
    {
        public static OllamaWslConfiguration FromConfiguration(IConfiguration cfg)
        {
            var ollamaPath = cfg["Wsl:OllamaPath"] ?? throw new InvalidOperationException("Wsl:OllamaPath is required");
            var password = cfg["Wsl:Password"] ?? throw new InvalidOperationException("Wsl:Password is required");
            var baseUrl = cfg["LLMAnalysis:Ollama:BaseUrl"] ?? throw new InvalidOperationException("LLMAnalysis:Ollama:BaseUrl is required");

            var models = OllamaSetupConfiguration.FromConfiguration(cfg).Models;

            return new OllamaWslConfiguration(
                Models: models,
                OllamaPath: ollamaPath,
                Password: password,
                BaseUrl: baseUrl);
        }
    }
    public sealed record OllamaRocmConfiguration(
        string Version,
        string VersionFull,
        string PackageVersion,
        string RepoBase,
        string UbuntuCodename,
        string InstallerDebUrl,
        string RocmLibPath
    )
    {

        public static OllamaRocmConfiguration FromConfiguration(IConfiguration cfg)
        {

            string version = cfg["Wsl:Rocm:Version"] ?? throw new InvalidOperationException("Wsl:Rocm:Version is required");
            string versionFull = cfg["Wsl:Rocm:VersionFull"] ?? throw new InvalidOperationException("Wsl:Rocm:VersionFull is required");
            string packageVersion = cfg["Wsl:Rocm:PackageVersion"] ?? throw new InvalidOperationException("Wsl:Rocm:PackageVersion is required");
            string repoBase = cfg["Wsl:Rocm:RepoBase"] ?? throw new InvalidOperationException("Wsl:Rocm:RepoBase is required");
            string ubuntuCodename = cfg["Wsl:Rocm:UbuntuCodename"] ?? throw new InvalidOperationException("Wsl:Rocm:UbuntuCodename is required");
            string installerDebUrl = $"{repoBase}/{version}/ubuntu/{ubuntuCodename}/amdgpu-install_{packageVersion}_all.deb";
            string rocmLibPath = $"/opt/rocm-{versionFull}/lib";

            return new OllamaRocmConfiguration(
                Version: version,
                VersionFull: versionFull,
                PackageVersion: packageVersion,
                RepoBase: repoBase,
                UbuntuCodename: ubuntuCodename,
                InstallerDebUrl: installerDebUrl,
                RocmLibPath: rocmLibPath
            );
        }

    }

    public sealed record OllamaServiceConfiguration(
        int Port,
        string LogPath,
        string TagsPath,
        string PsPath,
        string GeneratePath,
        string OllamaLibraryPath,
        string RocblasTensilePath)
    {

        public static OllamaServiceConfiguration FromConfiguration(IConfiguration cfg)
        {
            int port = int.TryParse(cfg["Wsl:Service:Port"], out var p) ? p : throw new InvalidOperationException("Wsl:Service:Port is required");
            string logPath = cfg["Wsl:Service:LogPath"] ?? throw new InvalidOperationException("Wsl:Service:LogPath is required");
            string tagsPath = cfg["Wsl:Service:TagsPath"] ?? throw new InvalidOperationException("Wsl:Service:TagsPath is required");
            string psPath = cfg["Wsl:Service:PsPath"] ?? throw new InvalidOperationException("Wsl:Service:PsPath is required");
            string generatePath = cfg["LLMAnalysis:Ollama:GeneratePath"] ?? throw new InvalidOperationException("LLMAnalysis:Ollama:GeneratePath is required");
            string ollamaLibraryPath = cfg["Wsl:Service:OllamaLibraryPath"] ?? "/usr/lib/ollama:/usr/lib/ollama/rocm";
            string rocblasTensilePath = cfg["Wsl:Service:RocblasTensilePath"] ?? "/usr/lib/ollama/rocm/rocblas/library";
            return new OllamaServiceConfiguration(
                Port: port,
                LogPath: logPath,
                TagsPath: tagsPath,
                PsPath: psPath,
                GeneratePath: generatePath,
                OllamaLibraryPath: ollamaLibraryPath,
                RocblasTensilePath: rocblasTensilePath
            );
        }

    }
        public sealed record OllamaRuntimeConfiguration(
           string KvCacheType,
           int NumParallel,
           bool FlashAttention)
        {
            public static OllamaRuntimeConfiguration FromConfiguration(IConfiguration cfg)
            {
                string kvCacheType = cfg["Wsl:Ollama:KvCacheType"] ?? "q8_0";
                int numParallel = int.TryParse(cfg["Wsl:Ollama:NumParallel"], out var n) ? n : 1;
                bool flashAttention = bool.TryParse(cfg["Wsl:Ollama:FlashAttention"], out var f) ? f : true;
                return new OllamaRuntimeConfiguration(
                    KvCacheType: kvCacheType,
                    NumParallel: numParallel,
                    FlashAttention: flashAttention
                );
            }
        }
    }
