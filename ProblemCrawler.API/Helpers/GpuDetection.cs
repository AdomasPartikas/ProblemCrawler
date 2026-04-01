using ProblemCrawler.Core.Enums;
using System.Management;
namespace ProblemCrawler.API.Helpers
{
    public class GpuDetection
    {
        public GpuDetection() { }

        public static GpuVendor DetectGpu()
        {
            if (OperatingSystem.IsWindows())
            {
                var searcher = new ManagementObjectSearcher("select Name, AdapterCompatibility from Win32_VideoController");
                if (searcher is null)
                {
                    return GpuVendor.unknown;
                }
                foreach (var mo in searcher.Get())
                {
                    string vendor = mo["AdapterCompatibility"]?.ToString() ?? string.Empty;

                    if (vendor.Contains("nvidia", StringComparison.OrdinalIgnoreCase))
                    {
                        return GpuVendor.nvidia;
                    }
                    if (vendor.Contains("amd", StringComparison.OrdinalIgnoreCase) || vendor.Contains("advanced micro devices", StringComparison.OrdinalIgnoreCase))
                    {
                        return GpuVendor.amd;
                    }

                }
                return GpuVendor.unknown;


            }
            if (OperatingSystem.IsLinux())
            {
                throw new NotImplementedException();
            }
            else
            {
                return GpuVendor.unknown;
            }
        }

    }
}
