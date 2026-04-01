namespace ProblemCrawler.Core.Scripts;

public static class OllamaScriptSections
{
    /// <summary>
    /// Generates the opening lines of the Ollama startup script.
    /// </summary>
    /// <returns>Shell script lines containing the shebang and an initial setup echo.</returns>
    public static IEnumerable<string> Preamble() =>
    [
        "#!/bin/bash",
        "echo '[ollama] Starting setup...'"
    ];
    /// <summary>
    /// Generates shell script lines that stop and kill any running Ollama instances before startup.
    /// </summary>
    /// <param name="password">The sudo password used to execute privileged commands.</param>
    /// <param name="port">The port number Ollama listens on, used to free any bound processes.</param>
    /// <returns>Shell script lines that stop the systemd service, kill stray processes, and free the port.</returns>
    public static IEnumerable<string> StopExistingInstances(string password, int port) =>
    [
        "echo '[ollama] Stopping any existing ollama instances...'",
        Sudo(password, $"systemctl stop ollama 2>/dev/null || true"),
        Sudo(password, $"systemctl disable ollama 2>/dev/null || true"),
        Sudo(password, $"killall -9 ollama 2>/dev/null || true"),
        Sudo(password, $"fuser -k {port}/tcp 2>/dev/null || true"),
        "sleep 3"
    ];
    /// <summary>
    /// Generates shell script lines that install the Ollama binary if it is not already present on the system.
    /// </summary>
    /// <param name="password">The sudo password used to execute privileged install commands.</param>
    /// <param name="ollamaPath">The expected file system path of the Ollama binary.</param>
    /// <returns>Shell script lines that check for the binary and run the official install script if missing.</returns>
    public static IEnumerable<string> InstallBinaryIfMissing(string password, string ollamaPath) =>
    [
        $"if [ ! -f {ollamaPath} ]; then",
        "  echo '[ollama] Binary not found, installing...'",
        $"  {Sudo(password, "bash -c 'apt-get update && apt-get install -y zstd curl && curl -fsSL https://ollama.com/install.sh | sh'")}",
        "else",
        "  echo '[ollama] Binary already installed, skipping.'",
        "fi"
    ];
    /// <summary>
    /// Generates shell script lines that install and configure Mesa Vulkan drivers inside WSL.
    /// </summary>
    /// <remarks>Includes cache clearing, environment variable resets, and two Vulkan probes —
    /// the first as a warmup and the second to verify the GPU is visible to the Vulkan loader.</remarks>
    /// <param name="password">The sudo password used to execute privileged driver installation commands.</param>
    /// <returns>Shell script lines covering Mesa installation, Vulkan environment setup, and GPU probing.</returns>
    public static IEnumerable<string> VulkanSetup(string password) =>
    [
        "if dpkg -l libvulkan1 mesa-vulkan-drivers vulkan-tools mesa-utils 2>/dev/null | grep -q '^ii' && apt-cache policy | grep -q 'kisak/kisak-mesa'; then",
        "    echo '[ollama] Mesa Vulkan + kisak PPA already installed, skipping.'",
        "else",
        "    echo '[ollama] Installing Mesa Vulkan...'",
        Sudo(password, "add-apt-repository ppa:kisak/kisak-mesa -y >/dev/null 2>&1 || true"),
        Sudo(password, "apt-get update -y >/dev/null 2>&1"),
        Sudo(password, "apt-get install -y libvulkan1 mesa-vulkan-drivers vulkan-tools mesa-utils || true"),
        "fi",
        "echo '[ollama] Forcing Vulkan loader refresh...'",
        "sudo ldconfig",
        "sleep 2",
        "rm -rf ~/.cache/vulkan 2>/dev/null || true",

        "echo '[ollama] Resetting Vulkan environment...'",
        "unset VK_ICD_FILENAMES",
        "unset VK_DRIVER_FILES",
        "unset VK_LAYER_PATH",
        "export MESA_LOADER_DRIVER_OVERRIDE=d3d12",
        "export VK_LOADER_DEBUG=warn",

        "echo '[ollama] Warming up WSL GPU bridge...'",
        "sleep 2",
        "ls /dev/dxg >/dev/null 2>&1 || true",

        "echo '[ollama] First Vulkan probe (warmup)...'",
        "vulkaninfo >/dev/null 2>&1 || true",
        "sleep 2",

        "echo '[ollama] Second Vulkan probe (real check)...'",
        "vulkaninfo --summary | grep -E 'deviceName|driverID|GPU' || true"
    ];
    /// <summary>
    /// Generates shell script lines that stop the Ollama service started automatically by the installer,
    /// so that the service can be restarted with custom GPU environment variables.
    /// </summary>
    /// <param name="password">The sudo password used to execute privileged stop commands.</param>
    /// <param name="port">The port number to free before restarting the service.</param>
    /// <returns>Shell script lines that stop the installer-managed service and free the port.</returns>
    public static IEnumerable<string> StopInstallerService(string password, int port) =>
    [
        "echo '[ollama] Stopping installer-started ollama service...'",
        Sudo(password, $"systemctl stop ollama 2>/dev/null || true"),
        Sudo(password, $"systemctl disable ollama 2>/dev/null || true"),
        Sudo(password, $"killall -9 ollama 2>/dev/null || true"),
        Sudo(password, $"fuser -k {port}/tcp 2>/dev/null || true"),
        "sleep 2"
    ];
    /// <summary>
    /// Generates shell script lines that start the Ollama server in the background with GPU-specific environment variables.
    /// </summary>
    /// <param name="ollamaPath">The file system path to the Ollama binary.</param>
    /// <param name="logPath">The file path where Ollama server output will be written.</param>
    /// <returns>Shell script lines that set GPU environment variables and launch the Ollama server via nohup.</returns>
    public static IEnumerable<string> ServeWithGpuEnvVars(string ollamaPath, string logPath) =>
    [
        "echo '[ollama] Starting ollama serve with GPU env vars...'",
        "unset OLLAMA_LLM_LIBRARY",
        "export OLLAMA_VULKAN=1",
        "export GGML_VK_VISIBLE_DEVICES=0",
        "export OLLAMA_DEBUG=0",
        $"nohup {ollamaPath} serve > {logPath} 2>&1 &"
    ];
    /// <summary>
    /// Generates shell script lines that poll the Ollama API until it becomes reachable.
    /// </summary>
    /// <param name="baseUrl">The base URL of the Ollama server.</param>
    /// <param name="tagsPath">The API path to poll for readiness (e.g. <c>/api/tags</c>).</param>
    /// <returns>Shell script lines that loop with a 2-second delay until the API responds successfully.</returns>
    public static IEnumerable<string> WaitForApi(string baseUrl, string tagsPath) =>
    [
        "echo '[ollama] Waiting for API...'",
        $"until curl -s {baseUrl}{tagsPath} > /dev/null 2>&1; do sleep 2; done",
        "echo '[ollama] API is ready.'"
    ];
    /// <summary>
    /// Generates shell script lines to check for and pull specified Ollama models using the provided Ollama executable
    /// path.
    /// </summary>
    /// <remarks>Each model results in a set of shell commands that first check for the model's presence and
    /// then pull it if it is missing. The generated script lines can be used in a shell environment to automate model
    /// management.</remarks>
    /// <param name="ollamaPath">The file system path to the Ollama executable used to pull models.</param>
    /// <param name="models">A collection of model names to check and pull if not already present.</param>
    /// <returns>An enumerable collection of shell script lines for each model, which check if the model is present and pull it
    /// if necessary.</returns>
    public static IEnumerable<string> PullModels(string ollamaPath, IEnumerable<string> models) =>
        models.SelectMany(model => new[]
        {
            $"if ollama list | grep -q '^{model}'; then",
            $"  echo '[ollama] Model {model} already present, skipping.'",
            $"else",
            $"  echo '[ollama] Pulling {model}...'",
            $"  {ollamaPath} pull {model}",
            $"fi"
        });
    /// <summary>
    /// Generates a shell command string that executes the specified command with elevated privileges using sudo and the
    /// provided password.
    /// </summary>
    /// <remarks>The returned string is intended for use in a shell environment where password-based sudo
    /// authentication is required. Use caution when handling passwords in command strings, as this approach may expose
    /// sensitive information.</remarks>
    /// <param name="password">The password to supply to sudo for authentication. Cannot be null or empty.</param>
    /// <param name="command">The shell command to execute with elevated privileges. Cannot be null or empty.</param>
    /// <returns>A shell command string that pipes the specified password to sudo and executes the given command.</returns>
    public static string Sudo(string password, string command) =>
        $"echo '{password}' | sudo -S {command}";
}