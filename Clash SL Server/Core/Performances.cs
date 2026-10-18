using System;
using System.Runtime.InteropServices;

namespace CSS.Core
{
    public static class PerformanceInfo
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct PerformanceInformation
        {
            public int Size;
            public IntPtr CommitTotal;
            public IntPtr CommitLimit;
            public IntPtr CommitPeak;
            public IntPtr PhysicalTotal;
            public IntPtr PhysicalAvailable;
            public IntPtr SystemCache;
            public IntPtr KernelTotal;
            public IntPtr KernelPaged;
            public IntPtr KernelNonPaged;
            public IntPtr PageSize;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;
        }

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPerformanceInfo([Out] out PerformanceInformation PerformanceInformation,
            [In] int Size);

        public static long GetPhysicalAvailableMemoryInMiB()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    var pi = new PerformanceInformation();
                    if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                        return Convert.ToInt64(pi.PhysicalAvailable.ToInt64() * pi.PageSize.ToInt64() / 1048576);
                }
                catch { }
            }
            return 4096;
        }

        public static long GetTotalMemoryInMiB()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    var pi = new PerformanceInformation();
                    if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                        return Convert.ToInt64(pi.PhysicalTotal.ToInt64() * pi.PageSize.ToInt64() / 1048576);
                }
                catch { }
            }
            return 8192;
        }
    }

    internal class Performances
    {
        public static string GetFreeMemory()
        {
            var phav = PerformanceInfo.GetPhysicalAvailableMemoryInMiB();
            var tot = PerformanceInfo.GetTotalMemoryInMiB();
            if (tot <= 0) return "50.00";
            var percentFree = phav / (decimal) tot * 100;
            return percentFree.ToString("##.##");
        }

        public static string GetFreeMemoryMB() => PerformanceInfo.GetPhysicalAvailableMemoryInMiB().ToString();

        public static string GetTotalMemory() => PerformanceInfo.GetTotalMemoryInMiB().ToString();

        public static string GetUsedMemory()
        {
            var phav = PerformanceInfo.GetPhysicalAvailableMemoryInMiB();
            var tot = PerformanceInfo.GetTotalMemoryInMiB();
            if (tot <= 0) return "50.00";
            var percentFree = phav / (decimal) tot * 100;
            var percentOccupied = 100 - percentFree;
            return percentOccupied.ToString("##.##");
        }
    }
}
