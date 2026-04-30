using System.Reflection;

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
        $"  {Sudo(password, "bash -lc 'apt-get update && apt-get install -y zstd curl'")}",
        $"  {Sudo(password, "bash -lc 'curl -fsSL https://ollama.com/download/ollama-linux-amd64.tar.zst | tar -I zstd -x -C /usr'")}",
        $"  {Sudo(password, "bash -lc 'curl -fsSL https://ollama.com/download/ollama-linux-amd64-rocm.tar.zst | tar -I zstd -x -C /usr'")}",
        "else",
        "  echo '[ollama] Binary already installed, skipping.'",
        "fi",
        "echo '[ollama] Verifying install layout...'",
        "ls -l /usr/bin/ollama 2>/dev/null || true",
        "ls -ld /usr/lib/ollama /usr/lib/ollama/rocm 2>/dev/null || true"
    ];
    /// <summary>
    /// Generates shell script lines that install ROCm userspace libraries for WSL2 using the new ROCDXG
    /// architecture (ROCm 7.2+). Requires Adrenalin 26.2.2+ on the Windows host.
    /// </summary>
    /// <remarks>
    /// Does NOT install the amdgpu kernel module (--no-dkms is mandatory in WSL2).
    /// The --usecase=wsl flag was removed in amdgpu-install 30.x; WSL support now ships via librocdxg.
    /// rocm-smi is unavailable in WSL2 by design — the Windows host driver owns the hardware.
    /// </remarks>
    /// <param name="password">The sudo password used to execute privileged commands.</param>
    /// <returns>Shell script lines that install the ROCm stack and verify GPU visibility via rocminfo.</returns>
    public static IEnumerable<string> RocmSetup(string password) =>
    [
        "echo '[ollama] Checking ROCm installation...'",

        "if ! command -v amdgpu-install >/dev/null 2>&1; then",
        "  echo '[ollama] Installing amdgpu-install for ROCm 7.2...'",
        $"  {Sudo(password, "bash -c 'wget -q https://repo.radeon.com/amdgpu-install/7.2/ubuntu/noble/amdgpu-install_7.2.70200-1_all.deb -O /tmp/amdgpu-install.deb && apt-get install -y /tmp/amdgpu-install.deb'")}",
        "else",
        "  echo '[ollama] amdgpu-install already present, skipping download.'",
        "fi",
        "echo '[ollama] Available AMD install usecases:'",
        $"{Sudo(password, "amdgpu-install --list-usecase || true")}",

        "echo '[ollama] Installing ROCm userspace (rocm only, no-dkms)...'",
        $"{Sudo(password, "amdgpu-install -y --usecase=wsl,rocm --no-dkms 2>/dev/null || true")}",

        "echo '[ollama] Checking Linux GPU access groups...'",
        "groups || true",
        "echo '[ollama] Adding current user to render/video groups if needed...'",
        $"{Sudo(password, "usermod -aG render,video $USER || true")}",

        "echo '[ollama] Probing ROCm GPU via rocminfo...'",
        "ROCM_RESULT=$(rocminfo 2>&1)",
        "echo \"$ROCM_RESULT\" | grep -E 'Agent|Device Type|Marketing Name' || true",

        "if echo \"$ROCM_RESULT\" | grep -q 'Device Type.*GPU'; then",
        "echo '[ollama] ROCm GPU detected successfully'",
        "echo '[ollama] Detecting GPU architecture...'",
        "GFX_TARGET=$(rocminfo 2>/dev/null | grep -m1 'Name:.*gfx' | grep -oP 'gfx\\d+' || true)",
        "echo \"[ollama] Detected GFX target: $GFX_TARGET\"",

        "if [ -n \"$GFX_TARGET\" ]; then",
        "  echo \"[ollama] Copying rocblas kernels for $GFX_TARGET from system ROCm into Ollama...\"",
        $"  {Sudo(password, "bash -c 'find /opt/rocm/lib/rocblas/library/ -name \"*${GFX_TARGET}*\" -exec cp {} /usr/lib/ollama/rocm/rocblas/library/ \\;'")}",
        "  ls /usr/lib/ollama/rocm/rocblas/library/ | grep \"$GFX_TARGET\" || echo \"[ollama] WARNING: no kernels found for $GFX_TARGET\"",
        "else",
        "  echo '[ollama] WARNING: could not detect GFX target, skipping kernel copy'",
        "fi",

        "echo '[ollama] Replacing Ollama bundled HSA runtime with WSL-aware system version...'",
        "HSA_SRC=$(find /opt/rocm-7.2.0/lib/ -name 'libhsa-runtime64.so.1.*' ! -type l | head -1)",
        "HSA_DST=$(find /usr/lib/ollama/rocm/ -name 'libhsa-runtime64.so.1.*' ! -type l | head -1)",
        "echo \"[ollama] Copying $HSA_SRC -> $HSA_DST\"",
        $"{Sudo(password, "bash -c 'HSA_SRC=$(find /opt/rocm-7.2.0/lib/ -name libhsa-runtime64.so.1.* ! -type l | head -1) && HSA_DST=$(find /usr/lib/ollama/rocm/ -name libhsa-runtime64.so.1.* ! -type l | head -1) && cp $HSA_SRC $HSA_DST'")}",
        $"{Sudo(password, "ldconfig")}",
        "ldd /usr/lib/ollama/rocm/libhsa-runtime64.so.1 | grep -i dxcore || echo 'WARNING: dxcore not linked'",
        "else",
        "echo '[ollama] ROCm GPU not detected — will fall back to Vulkan'",
        "fi"
    ];
    /// <summary>
    /// Generates shell script lines that start Ollama using native ROCm (no Vulkan).
    /// Should only be called when ROCm GPU detection succeeded.
    /// </summary>
    /// <param name="ollamaPath">The file system path to the Ollama binary.</param>
    /// <param name="logPath">The file path where Ollama server output will be written.</param>
    /// <returns>Shell script lines that set ROCm env vars and launch Ollama via nohup.</returns>
    public static IEnumerable<string> ServeWithRocmEnvVars(string ollamaPath, string logPath, int contextSize) =>
    [
        "echo '[ollama] Starting ollama serve with ROCm env vars...'",
        "unset OLLAMA_VULKAN",
        "export OLLAMA_LLM_LIBRARY=rocm",
        "export OLLAMA_FLASH_ATTENTION=true",
        $"export OLLAMA_CONTEXT_LENGTH={contextSize}",
        "export OLLAMA_KV_CACHE_TYPE=q8_0",
        "export OLLAMA_NUM_PARALLEL=1",
        "export OLLAMA_LIBRARY_PATH=/usr/lib/ollama:/usr/lib/ollama/rocm",
        "export LD_LIBRARY_PATH=/usr/lib/ollama:/usr/lib/ollama/rocm:/opt/rocm-7.2.0/lib:${LD_LIBRARY_PATH}",
        "export ROCBLAS_TENSILE_LIBPATH=/usr/lib/ollama/rocm/rocblas/library",
        "export HSA_ENABLE_ROCDXG=1",
        "echo '[ollama] Verifying libggml-hip dependencies...'",
        "ldd /usr/lib/ollama/rocm/libggml-hip.so | grep -E 'not found|ggml|hip|roc|hsa|blas' || true",
        $"nohup {ollamaPath} serve > {logPath} 2>&1 &"
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
        Sudo(password, "ldconfig"),
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
    public static IEnumerable<string> ServeWithGpuEnvVars(string ollamaPath, string logPath, int contextSize) =>
    [
        "echo '[ollama] Starting ollama serve with GPU env vars...'",
        "unset OLLAMA_LLM_LIBRARY",
        "export OLLAMA_VULKAN=1",
        "export OLLAMA_FLASH_ATTENTION=true",
        "export OLLAMA_KV_CACHE_TYPE=q8_0",
        $"export OLLAMA_CONTEXT_LENGTH={contextSize}",
        "export OLLAMA_NUM_PARALLEL=1",
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