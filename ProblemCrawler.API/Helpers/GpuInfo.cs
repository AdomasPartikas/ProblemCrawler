using Vortice.DXGI;

namespace ProblemCrawler.API.Helpers
{
    public static class GpuInfo
    {
        public static long GetVramBytes(ILogger logger)
        {
            try
            {
                DXGI.CreateDXGIFactory1(out IDXGIFactory1? factory).CheckError();

                if(factory is null)
                {
                    return 0;
                }

                for (uint i = 0; factory.EnumAdapters1(i, out IDXGIAdapter1? adapter).Success; i++)
                {
                    if (adapter is null) continue;

                    var desc = adapter.Description1;
                    var name = desc.Description;
                    var vram = (long)(ulong)desc.DedicatedVideoMemory;

                    logger.LogInformation("[GpuInfo] Adapter {I}: {Name} | VRAM: {GB} GB | Flags: {Flags}",
                        i, name, vram / 1024 / 1024 / 1024, desc.Flags);

                    bool isDiscrete = vram > 0 && !desc.Flags.HasFlag(AdapterFlags.Software);

                    if (isDiscrete)
                    {
                        logger.LogInformation("[GpuInfo] Selected discrete GPU: {Name} | VRAM: {GB} GB",
                            name, vram / 1024 / 1024 / 1024);
                        adapter.Dispose();
                        factory.Dispose();
                        return vram;
                    }

                    adapter.Dispose();
                }

                factory.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[GpuInfo] DXGI VRAM detection failed");
            }

            logger.LogWarning("[GpuInfo] No discrete GPU found, letting Ollama decide");
            return 0;
        }

        public static int GetOptimalContextSize(long vramBytes)
        {
            var mb = vramBytes / 1024 / 1024;

            return mb switch
            {
                >= 14000 => 32768,
                >= 10000 => 16384,
                >= 6000 => 8192,
                >= 4000 => 4096,
                _ => 4096
            };
        }
        public static int GetParallelContextSize(long vramBytes, int numParallel)
        {
            var baseContext = GetOptimalContextSize(vramBytes);
            var adjusted = baseContext / Math.Max(1, numParallel);
            var pow2 = 1;
            while (pow2 * 2 <= adjusted) pow2 *= 2;
            return Math.Max(4096, pow2);
        }
    }
}

