﻿
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace IoTSharp.Extensions
{

    public static class MiscExtensions
    {
        public static IWebHostBuilder UseJsonToSettings(this IWebHostBuilder hostBuilder, string filename)
        {
            return hostBuilder.ConfigureAppConfiguration(builder =>
            {
                try
                {
                    if (System.IO.File.Exists(filename))
                    {
                        builder.AddJsonFile(filename, true);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            });
        }
        public static IWebHostBuilder UseContentRootAsEnv(this IWebHostBuilder hostBuilder)
        {
            bool IsWindowsService = false;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var process = GetParent(Process.GetCurrentProcess()))
                {
                    IsWindowsService = process != null && process.ProcessName == "services";
                }
            }
            if (Environment.CommandLine.Contains("--usebasedirectory") || (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && IsWindowsService))
            {
                hostBuilder.UseContentRoot(AppContext.BaseDirectory);
            }
            else
            {
                if (!Debugger.IsAttached)
                {
                    hostBuilder.UseContentRoot(System.IO.Directory.GetCurrentDirectory());
                }
            }
            return hostBuilder;
        }

        public static void RunAsEnv(this IHost host)
        {
            bool IsWindowsService = false;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var process = GetParent(Process.GetCurrentProcess()))
                {
                    IsWindowsService = process != null && process.ProcessName == "services";
                }
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && IsWindowsService)
            {
                System.IO.Directory.SetCurrentDirectory(AppContext.BaseDirectory);
                host.Run();
            }
            else
            {
                if (Environment.CommandLine.Contains("--usebasedirectory"))
                {
                    System.IO.Directory.SetCurrentDirectory(AppContext.BaseDirectory);
                }
                host.Run();
            }
        }
        private static Process GetParent(Process child)
        {
            var parentId = 0;

            var handle = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);

            if (handle == IntPtr.Zero)
            {
                return null;
            }

            var processInfo = new PROCESSENTRY32
            {
                dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32))
            };

            if (!Process32First(handle, ref processInfo))
            {
                return null;
            }

            do
            {
                if (child.Id == processInfo.th32ProcessID)
                {
                    parentId = (int)processInfo.th32ParentProcessID;
                }
            } while (parentId == 0 && Process32Next(handle, ref processInfo));

            if (parentId > 0)
            {
                return Process.GetProcessById(parentId);
            }
            return null;
        }

        private static uint TH32CS_SNAPPROCESS = 2;

        [DllImport("kernel32.dll")]
        public static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        public static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        public static string MD5Sum(this string text) => BitConverter.ToString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
        public static Task Forget(this Task task)
        {
            return Task.CompletedTask;
        }
        public static Task StartSTATask(this TaskFactory task, Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(new object());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public static T GetRequiredService<T>(this IServiceScopeFactory scopeFactor) =>
                                    scopeFactor.CreateScope().ServiceProvider.GetRequiredService<T>();
    }

}

