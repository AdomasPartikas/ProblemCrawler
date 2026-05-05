using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProblemCrawler.Core.Interfaces;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ProblemCrawler.API.Helpers
{
    public sealed class ClusterJobRunner : IClusterJobRunner
    {
        private static readonly Regex AnsiEscapePattern =
            new(@"\x1B\[[^@-~]*[@-~]|[^\x20-\x7E\n]", RegexOptions.Compiled);

        private readonly ILogger<ClusterJobRunner> _logger;
        private readonly IConfiguration _configuration;

        public ClusterJobRunner(ILogger<ClusterJobRunner> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                var config = LoadConfig();
                var clusterFolder = ResolveClusterFolder();

                await EnsureImageAsync(config.ImageName, clusterFolder, cancellationToken);
                await RunContainerAsync(config, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[cluster] Cluster job was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[cluster] Cluster job failed");
            }
        }

        private ClusterConfig LoadConfig()
        {
            var connectionString = Require("DatabaseSettings:ConnectionString");
            var db = ParseConnectionString(connectionString);

            return new ClusterConfig(
                ImageName: Require("Clustering:Docker:ImageName"),
                Db: db,
                MinClusterSize: Require("Clustering:Hdbscan:MinClusterSize"),
                SourceTable: Require("Clustering:SourceTable"),
                IdColumn: Require("Clustering:IdColumn"),
                EmbeddingColumn: Require("Clustering:EmbeddingColumn"),
                ClusterIdColumn: Require("Clustering:ClusterIdColumn"),
                ClusterConfColumn: Require("Clustering:ClusterConfidenceColumn"),
                ClusterRunIdColumn: Require("Clustering:ClusterRunIdColumn"),
                ClusterRunTable: Require("Clustering:ClusterRunTable"),
                ProblemClusterTable: Require("Clustering:ProblemClusterTable"),
                IdeaTable: Require("Clustering:IdeaTable"),
                EmbeddingArchiveTable: Require("Clustering:EmbeddingArchiveTable"),
                ProblemClusterArchiveTable: Require("Clustering:ProblemClusterArchiveTable"),
                MaxClusterRuns: Require("Clustering:MaxClusterRuns"),
                Schema: Require("Clustering:Schema")
            );
        }

        private static string ResolveClusterFolder()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\.."));
            var clusterFolder = Path.Combine(projectRoot, "Docker", "ClusterJob");

            if (!Directory.Exists(clusterFolder))
                throw new DirectoryNotFoundException($"ClusterJob folder not found: {clusterFolder}");

            return clusterFolder;
        }

        private string Require(string key) =>
            _configuration[key] ?? throw new InvalidOperationException($"Missing configuration key: {key}");


        private async Task EnsureImageAsync(string imageName, string clusterFolder, CancellationToken cancellationToken)
        {
            if (await ImageExistsAsync(imageName, cancellationToken))
            {
                _logger.LogInformation("[cluster] Image {ImageName} already exists, skipping build", imageName);
                return;
            }

            _logger.LogInformation("[cluster] Building image {ImageName}", imageName);
            await RunProcess(
                "cmd.exe",
                $"/c docker build -t {imageName} .",
                clusterFolder,
                hideArguments: false,
                cancellationToken);

            _logger.LogInformation("[cluster] Image build completed");
        }
        private async Task<bool> ImageExistsAsync(string imageName, CancellationToken cancellationToken)
        {
            var exitCode = await RunProcess(
                "cmd.exe",
                $"/c docker image inspect {imageName}",
                null,
                hideArguments: true,
                cancellationToken,
                throwOnNonZeroExit: false);

            return exitCode == 0;
        }
        private async Task RunContainerAsync(ClusterConfig config, CancellationToken cancellationToken)
        {
            var envArgs = BuildDockerEnvArgs(new Dictionary<string, string?>
            {
                ["DB_HOST"] = config.Db.Host,
                ["DB_PORT"] = config.Db.Port,
                ["DB_NAME"] = config.Db.Database,
                ["DB_USER"] = config.Db.Username,
                ["DB_PASSWORD"] = config.Db.Password,
                ["DB_SSLMODE"] = config.Db.SslMode,
                ["HDBSCAN_MIN_CLUSTER_SIZE"] = config.MinClusterSize,
                ["SOURCE_TABLE"] = config.SourceTable,
                ["ID_COLUMN"] = config.IdColumn,
                ["EMBEDDING_COLUMN"] = config.EmbeddingColumn,
                ["CLUSTER_ID_COLUMN"] = config.ClusterIdColumn,
                ["CLUSTER_CONFIDENCE_COLUMN"] = config.ClusterConfColumn,
                ["CLUSTER_RUN_ID_COLUMN"] = config.ClusterRunIdColumn,
                ["CLUSTER_RUN_TABLE"] = config.ClusterRunTable,
                ["PROBLEM_CLUSTER_TABLE"] = config.ProblemClusterTable,
                ["IDEA_TABLE"] = config.IdeaTable,
                ["EMBEDDING_ARCHIVE_TABLE"] = config.EmbeddingArchiveTable,
                ["PROBLEM_CLUSTER_ARCHIVE_TABLE"] = config.ProblemClusterArchiveTable,
                ["MAX_CLUSTER_RUNS"] = config.MaxClusterRuns,
                ["DB_SCHEMA"] = config.Schema
            });

            _logger.LogInformation("[cluster] Starting cluster job");
            var exitCode = await RunProcess("cmd.exe", $"/c docker run --rm {envArgs} {config.ImageName}", null, hideArguments: false, cancellationToken, throwOnNonZeroExit: false);

            if (exitCode != 0)
            {
                _logger.LogError("[cluster] Cluster job failed with exit code {ExitCode}", exitCode);
                return;
            }

            _logger.LogInformation("[cluster] Cluster job completed");
        }


        private async Task<int> RunProcess(
            string fileName,
            string arguments,
            string? workingDirectory,
            bool hideArguments,
            CancellationToken cancellationToken,
            bool throwOnNonZeroExit = true,
            LogLevel stderrLevel = LogLevel.Debug)
        {
            if (hideArguments)
                _logger.LogDebug("Running: {FileName} [arguments hidden]", fileName);
            else
                _logger.LogDebug("Running: {FileName} {Arguments}", fileName, arguments);

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? string.Empty
            };

            using var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    _logger.LogInformation("[cluster][stdout] {Line}", StripAnsi(e.Data));
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    _logger.LogError("[cluster][stderr] {Line}", StripAnsi(e.Data));
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {

                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch { }
                throw;
            }

            if (process.ExitCode != 0 && throwOnNonZeroExit)
                throw new InvalidOperationException($"Process '{fileName}' exited with code {process.ExitCode}");

            return process.ExitCode;
        }

        private static string StripAnsi(string input) =>
            AnsiEscapePattern.Replace(input, string.Empty).Trim();

        private static string BuildDockerEnvArgs(IReadOnlyDictionary<string, string?> env)
        {
            var sb = new StringBuilder();
            foreach (var kv in env)
            {
                if (string.IsNullOrWhiteSpace(kv.Value)) continue;
                sb.Append($" -e {kv.Key}={EscapeForCmd(kv.Value)}");
            }
            return sb.ToString();
        }

        private static string EscapeForCmd(string value) =>
            $"\"{value.Replace("\"", "\\\"")}\"";

        private static DbConnectionParams ParseConnectionString(string connectionString)
        {
            var parts = connectionString
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim().ToLowerInvariant(), p => p[1].Trim());

            return new DbConnectionParams(
                Host: parts.GetValueOrDefault("host") ?? throw new InvalidOperationException("Missing Host in connection string"),
                Port: parts.GetValueOrDefault("port") ?? "5432",
                Database: parts.GetValueOrDefault("database") ?? throw new InvalidOperationException("Missing Database in connection string"),
                Username: parts.GetValueOrDefault("username") ?? throw new InvalidOperationException("Missing Username in connection string"),
                Password: parts.GetValueOrDefault("password") ?? throw new InvalidOperationException("Missing Password in connection string"),
                SslMode: parts.GetValueOrDefault("sslmode") ?? throw new InvalidOperationException("Missing SslMode in connection string")
            );
        }

        private sealed record DbConnectionParams(
            string Host,
            string Port,
            string Database,
            string Username,
            string Password,
            string SslMode);

        private sealed record ClusterConfig(
            string ImageName,
            DbConnectionParams Db,
            string MinClusterSize,
            string SourceTable,
            string IdColumn,
            string EmbeddingColumn,
            string ClusterIdColumn,
            string ClusterConfColumn,
            string ClusterRunIdColumn,
            string ClusterRunTable,
            string ProblemClusterTable,
            string IdeaTable,
            string EmbeddingArchiveTable,
            string ProblemClusterArchiveTable,
            string MaxClusterRuns,
            string Schema);
    }
}

