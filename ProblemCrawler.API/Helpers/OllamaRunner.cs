using ProblemCrawler.Core.Enums;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Scripts;

namespace ProblemCrawler.API.Helpers
{
    public static class OllamaRunner
    {
        private const string OllamaApiTagsPath = "/api/tags";
        private const string OllamaApiPsPath = "/api/ps";
        private const string OllamaApiGenPath = "/api/generate";
        private const int OllamaPort = 11434;
        private const string OllamaLogPath = "/tmp/ollama.log";
        private static readonly Regex AnsiEscapePattern =
        new(@"\x1B\[[^@-~]*[@-~]|[^\x20-\x7E\n]", RegexOptions.Compiled);

        /// <summary>
        /// Initializes and starts the Ollama service using the specified GPU vendor, logging, and configuration
        /// settings.
        /// </summary>
        /// <remarks>Due to Ollama support for AMD GPUs and wsl rocm limitations, the method configures and starts Ollama within a WSL environment,
        /// including Vulkan setup and model pulling. For other GPU vendors, it uses Docker Compose to build and start
        /// the service. The method logs progress and errors throughout the process.</remarks>
        /// <param name="gpu">The GPU vendor to use for running Ollama. Determines whether to use AMD-specific setup or a Docker-based
        /// approach.</param> Local GPU vendor
        /// <param name="logger">The logger instance used to record informational and error messages during the setup and startup process.</param> 
        /// <param name="configuration">The configuration source containing required settings such as model names and, for AMD, WSL credentials.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task RunOllama(
            GpuVendor gpu,
            ILogger logger,
            IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            if (gpu == GpuVendor.amd || gpu == GpuVendor.unknown)
            {
                await RunOllamaWsl(logger, configuration, cancellationToken);
            }
            if (gpu == GpuVendor.nvidia)
            {
                await RunOllamaDocker(gpu, logger, configuration, cancellationToken);
            }
        }
        /// <summary>
        /// Starts the Ollama service inside a WSL environment, verifies its availability, and logs the process output.
        /// </summary>
        /// <remarks>If Ollama verification fails after startup, an error is logged but no exception is
        /// thrown to the caller.</remarks>
        /// <param name="logger">The logger used to record informational and error messages during the Ollama startup and verification
        /// process.</param>
        /// <param name="configuration">The configuration source containing settings required to initialize and run Ollama within WSL.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task RunOllamaWsl(
        ILogger logger,
        IConfiguration configuration,
        CancellationToken cancellationToken)
        {
            var settings = OllamaWslConfiguration.FromConfiguration(configuration);

            var scriptPath = await WriteBashScript(settings, cancellationToken);
            var wslScriptPath = ToWslPath(scriptPath);

            await RunProcess("wsl.exe", $"-e bash {wslScriptPath}", workingDirectory: null, logger, cancellationToken);
            await RunProcess("wsl.exe", $"-e bash -c \"cat {OllamaLogPath}\"", workingDirectory: null, logger, cancellationToken);

            try
            {
                await VerifyOllama(settings.BaseUrl, settings.Models.First(), logger, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ollama] Verification failed after startup");
            }
        }
        /// <summary>
        /// Stops the Ollama service using the appropriate shutdown procedure for the specified GPU vendor.
        /// </summary>
        /// <remarks>For AMD or unknown GPU vendors, the method stops Ollama instances running in WSL. For
        /// NVIDIA GPUs, it stops Docker-based Ollama instances. This method logs the shutdown process and is intended
        /// for use in environments where Ollama is managed programmatically.</remarks>
        /// <param name="gpu">The GPU vendor to target when stopping Ollama. Determines which shutdown process is used.</param>
        /// <param name="logger">The logger used to record informational messages during the shutdown process. Cannot be null.</param>
        /// <param name="configuration">The application configuration used to retrieve Ollama-specific settings. Cannot be null.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation is canceled if the token is triggered.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public static async Task StopOllama(GpuVendor gpu,
            ILogger logger,
            IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("[ollama] Shutting down Ollama...");
            if (gpu == GpuVendor.amd || gpu == GpuVendor.unknown)
            {
                var settings = OllamaWslConfiguration.FromConfiguration(configuration);
                var StopScript = string.Join('\n', OllamaScriptSections.StopExistingInstances(settings.Password, OllamaPort));
                await RunProcess("wsl.exe", $"-e bash -c \"{StopScript}\"", workingDirectory: null, logger, cancellationToken);
            }
            if (gpu == GpuVendor.nvidia)
            {
                var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\.."));
                var dockerFolder = Path.Combine(projectRoot, "Docker");
                await RunProcess("cmd.exe", "/c docker compose down", dockerFolder, logger, cancellationToken);
            }
            logger.LogInformation("[ollama] Ollama stopped");
        }
        /// <summary>
        /// Creates a temporary Bash script file based on the specified configuration and returns the path to the
        /// script.
        /// </summary>
        /// <remarks>The script is written to the system's temporary directory and will be overwritten if
        /// called multiple times in the same process.</remarks>
        /// <param name="s">The configuration used to generate the contents of the Bash script.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A string containing the full path to the generated Bash script file.</returns>
        private static async Task<string> WriteBashScript(OllamaWslConfiguration configuration, CancellationToken cancellationToken)
        {
            string[] lines =
            [
                .. OllamaScriptSections.Preamble(),
                .. OllamaScriptSections.StopExistingInstances(configuration.Password, OllamaPort),
                .. OllamaScriptSections.InstallBinaryIfMissing(configuration.Password, configuration.OllamaPath),
                .. OllamaScriptSections.VulkanSetup(configuration.Password),
                .. OllamaScriptSections.StopInstallerService(configuration.Password, OllamaPort),
                .. OllamaScriptSections.ServeWithGpuEnvVars(configuration.OllamaPath, OllamaLogPath),
                .. OllamaScriptSections.WaitForApi(configuration.BaseUrl, OllamaApiTagsPath),
                .. OllamaScriptSections.PullModels(configuration.OllamaPath, configuration.Models)
            ];

            var scriptPath = Path.Combine(Path.GetTempPath(), "ollama_start.sh");
            await File.WriteAllTextAsync(scriptPath, string.Join('\n', lines), cancellationToken);
            return scriptPath;
        }

        /// <summary>
        /// Converts a Windows file system path to its equivalent Windows Subsystem for Linux (WSL) path format.
        /// </summary>
        /// <param name="windowsPath">The Windows file system path to convert. Must be an absolute path including a drive letter (e.g.,
        /// "C:\\Users\\").</param>
        /// <returns>A string containing the WSL-formatted path corresponding to the specified Windows path.</returns>
        private static string ToWslPath(string windowsPath) =>
            "/mnt/" + windowsPath.Replace(":\\", "/").Replace('\\', '/').ToLowerInvariant();

        /// <summary>
        /// Builds and starts the Ollama Docker containers using the specified GPU profile.
        /// </summary>
        /// <remarks>This method invokes Docker Compose commands to build and start containers with the
        /// selected GPU profile. It is intended for use in automated deployment or setup scenarios.</remarks>
        /// <param name="gpu">The GPU vendor profile to use when building and running the Docker containers.</param>
        /// <param name="logger">The logger used to record informational and error messages during the Docker operation.</param>
        /// <param name="configuration">The application configuration settings. Not directly used in this method but may be required for future
        /// extensions.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation is canceled if the token is triggered.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task RunOllamaDocker(
            GpuVendor gpu,
            ILogger logger,
            IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\.."));
            var dockerFolder = Path.Combine(projectRoot, "Docker");
            var profile = gpu.ToString();
            var models = OllamaSetupConfiguration.FromConfiguration(configuration).Models;
            var modelsList = string.Join(",", models);

            await RunProcess(
                "cmd.exe",
                $"/c set OLLAMA_MODELS={modelsList} && docker compose --profile {profile} build --no-cache && docker compose --profile {profile} up -d",
                dockerFolder,
                logger,
                cancellationToken);
        }

        /// <summary>
        /// Verifies connectivity to an Ollama server and performs a warm-up operation using the specified model.
        /// </summary>
        /// <param name="baseUrl">The base URL of the Ollama server to connect to. Must be a valid HTTP or HTTPS endpoint.</param>
        /// <param name="warmupModel">The name of the model to use for the warm-up operation. This model will be loaded to ensure readiness.</param>
        /// <param name="logger">The logger used to record VRAM usage and diagnostic information during verification.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the verification operation.</param>
        /// <returns>A task that represents the asynchronous verification operation.</returns>

        private static async Task VerifyOllama(
            string baseUrl,
            string warmupModel,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

            await WarmUpModel(http, warmupModel, cancellationToken);
            await LogVramUsage(http, logger, cancellationToken);
        }
        /// <summary>
        /// Sends a test request to the specified model endpoint to initialize or warm up the model for subsequent use.
        /// </summary>
        /// <remarks>This method can be used to reduce latency for the first request to a model by
        /// ensuring it is loaded and ready to serve requests.</remarks>
        /// <param name="http">The HTTP client used to send the warm-up request.</param>
        /// <param name="model">The name of the model to warm up.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the warm-up operation.</param>
        /// <returns>A task that represents the asynchronous warm-up operation.</returns>
        private static async Task WarmUpModel(HttpClient http, string model, CancellationToken cancellationToken)
        {
            var body = new StringContent(
                $$"""{"model":"{{model}}","prompt":"hi","stream":false}""",
                Encoding.UTF8,
                "application/json");

            await http.PostAsync(OllamaApiGenPath, body, cancellationToken);
        }
        /// <summary>
        /// Logs the VRAM usage and model information retrieved from the Ollama API endpoint.
        /// </summary>
        /// <remarks>If no models are reported by the API, a warning is logged and no VRAM usage
        /// information is available. The method distinguishes between GPU and CPU usage based on the VRAM
        /// value.</remarks>
        /// <param name="http">The HTTP client used to send the request to the Ollama API.</param>
        /// <param name="logger">The logger used to record VRAM usage and status messages.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous logging operation.</returns>
        private static async Task LogVramUsage(HttpClient http, ILogger logger, CancellationToken cancellationToken)
        {
            var response = await http.GetAsync(OllamaApiPsPath, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(json);
            var models = doc.RootElement.GetProperty("models").EnumerateArray().ToList();

            if (models.Count == 0)
            {
                logger.LogWarning("[ollama] No models reported by /api/ps — cannot verify VRAM usage");
                return;
            }

            var model = models[0];
            var name = model.GetProperty("name").GetString();
            var sizeVram = model.GetProperty("size_vram").GetInt64();
            var size = model.GetProperty("size").GetInt64();

            logger.LogInformation(
                "[ollama] Model: {Name} | VRAM: {Vram} MB | Total: {Total} MB",
                name, sizeVram / 1024 / 1024, size / 1024 / 1024);

            if (sizeVram > 0)
                logger.LogInformation("[ollama] GPU is ACTIVE");
            else
                logger.LogWarning("[ollama] Running on CPU — GPU not being used!");
        }

        /// <summary>
        /// Runs an external process asynchronously with the specified arguments and logs its output and error streams.
        /// </summary>
        /// <remarks>Standard output and error streams are logged at debug level. If the process exits with
        /// a non-zero code, an error is logged. The method does not throw if the process fails, but logs the exit
        /// code.</remarks>
        /// <param name="fileName">The path to the executable file to run. Cannot be null or empty.</param>
        /// <param name="arguments">The command-line arguments to pass to the process. May be an empty string if no arguments are required.</param>
        /// <param name="workingDirectory">The working directory for the process, or null to use the current directory.</param>
        /// <param name="logger">The logger used to record process output, error messages, and status information. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the process execution.</param>
        /// <returns>A task that represents the asynchronous operation. The task completes when the process exits.</returns>
        private static async Task RunProcess(
            string fileName,
            string arguments,
            string? workingDirectory,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            logger.LogDebug("Running: {FileName} {Arguments}", fileName, arguments);

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

            process.OutputDataReceived += (_, e) => { if (e.Data is not null) logger.LogDebug("{Line}", StripAnsi(e.Data)); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) logger.LogDebug("{Line}", StripAnsi(e.Data)); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                logger.LogError("[ollama] Process exited with code {Code}", process.ExitCode);
                throw new InvalidOperationException(
                    $"Process '{fileName} {arguments}' exited with code {process.ExitCode}");
            }
            else
                logger.LogDebug("[ollama] Process finished successfully");
        }
        /// <summary>
        /// Removes ANSI escape sequences from the specified string.
        /// </summary>
        /// <param name="input">The input string that may contain ANSI escape sequences to be removed. Cannot be null.</param>
        /// <returns>A string with all ANSI escape sequences removed and leading or trailing whitespace trimmed. Returns an empty
        /// string if the input contains only ANSI sequences or whitespace.</returns>
        private static string StripAnsi(string input) =>
            AnsiEscapePattern.Replace(input, string.Empty).Trim();
    }

}
