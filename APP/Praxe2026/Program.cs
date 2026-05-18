using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Praxe2026
{
    public class AppEntry
    {
        public string Name { get; set; }
        public string UninstallString { get; set; }
    }

    public class ServerLists
    {
        public List<string> whitelist { get; set; }
        public List<string> blacklist { get; set; }
    }

    public class AdminCredentials
    {
        [System.Text.Json.Serialization.JsonPropertyName("user")]
        public string User { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("pass")]
        public string Pass { get; set; }
    }

    [System.Text.Json.Serialization.JsonSerializable(typeof(ServerLists))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(List<string>))]
    internal partial class AppJsonSerializerContext : System.Text.Json.Serialization.JsonSerializerContext
    {
    }

    class Program
    {
        // --- CONFIGURATION ---
        private const string ServerUrl = "https://praxe2026.milos-scripts.xyz"; // Updated to use production domain
        // ---------------------

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        static async Task Main(string[] args)
        {
            if (!IsAdministrator())
            {
                Console.WriteLine("Requesting Administrator privileges...");
                RunAsAdmin();
                return;
            }

            Console.WriteLine("=== Praxe2026 Provisioning & Cleanup Tool ===");
            
            TriggerWindowsUpdates();
            //await ProcessApplicationsAsync();
            await ProcessInstallersAsync();
            await ProcessPostInstallAsync();
            
            Console.WriteLine("\nAll tasks completed.");
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                long total = drive.TotalSize;
                long free = drive.AvailableFreeSpace;
                long used = total - free;
                
                double totalGb = total / 1073741824.0;
                double usedGb = used / 1073741824.0;
                double percentFree = (double)free / total * 100;
                
                string label = string.IsNullOrEmpty(drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel;
                Console.WriteLine($"{drive.Name} [{label}] {usedGb:F2}GB/{totalGb:F2}GB ({percentFree:F1}% free)");
            }

            Console.WriteLine("\nCalculating size of C:\\Users (please wait)...");
            try 
            {
                var options = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true };
                long totalUsersSize = new DirectoryInfo(@"C:\Users").EnumerateFiles("*", options).Sum(f => f.Length);
                Console.WriteLine($"Total size of C:\\Users: {totalUsersSize / 1073741824.0:F2} GB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not calculate size: {ex.Message}");
            }
        }

        static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void RunAsAdmin()
        {
            var exeName = Process.GetCurrentProcess().MainModule.FileName;

            ProcessStartInfo startInfo = new ProcessStartInfo(exeName)
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            try { Process.Start(startInfo); } catch { /* User denied UAC */ }
        }

        static void TriggerWindowsUpdates()
        {
            Console.WriteLine("\n[1] Checking for Windows Updates...");
            try
            {
                Process.Start(new ProcessStartInfo("UsoClient.exe", "ScanInstallWait")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                Console.WriteLine("Updates triggered successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to trigger updates: {ex.Message}");
            }
        }

        static async Task ProcessInstallersAsync()
        {
            Console.WriteLine("\n[2] Processing automatic installations...");
            using HttpClient client = new HttpClient();
            string tempDir = Path.GetTempPath();

            // Process MSIs
            try
            {
                var msiFiles = await client.GetFromJsonAsync($"{ServerUrl}/api/files/installers/msi", AppJsonSerializerContext.Default.ListString);
                if (msiFiles != null && msiFiles.Any())
                {
                    foreach (var msi in msiFiles)
                    {
                        Console.WriteLine($"Downloading MSI: {msi}...");
                        byte[] fileBytes = await client.GetByteArrayAsync($"{ServerUrl}/static/installers/msi/{msi}");
                        string tempPath = Path.Combine(tempDir, msi);
                        await File.WriteAllBytesAsync(tempPath, fileBytes);

                        Console.WriteLine($"Installing {msi} silently...");
                        using (Process process = Process.Start(new ProcessStartInfo("msiexec.exe", $"/i \"{tempPath}\" /qn /norestart") { UseShellExecute = false, CreateNoWindow = true }))
                        {
                            process?.WaitForExit();
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Failed to process MSIs: {ex.Message}"); }

            // Process EXEs
            try
            {
                var exeFiles = await client.GetFromJsonAsync($"{ServerUrl}/api/files/installers/exe", AppJsonSerializerContext.Default.ListString);
                if (exeFiles != null && exeFiles.Any())
                {
                    foreach (var exe in exeFiles)
                    {
                        Console.WriteLine($"Downloading EXE: {exe}...");
                        byte[] fileBytes = await client.GetByteArrayAsync($"{ServerUrl}/static/installers/exe/{exe}");
                        string tempPath = Path.Combine(tempDir, exe);
                        await File.WriteAllBytesAsync(tempPath, fileBytes);

                        Console.WriteLine($"Installing {exe} silently...");
                        using (Process process = Process.Start(new ProcessStartInfo(tempPath, "/S /quiet /norestart") { UseShellExecute = false, CreateNoWindow = true }))
                        {
                            process?.WaitForExit();
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Failed to process EXEs: {ex.Message}"); }
        }

        static async Task ProcessPostInstallAsync()
        {
            Console.WriteLine("\n[3] Running post-install executables...");
            
            // Open Settings to Windows Update Tab and force a scan
            try
            {
                Console.WriteLine("Opening Windows Update Settings and triggering scan...");
                Process.Start(new ProcessStartInfo("ms-settings:windowsupdate-action") { UseShellExecute = true });
            }
            catch (Exception ex) { Console.WriteLine($"Could not open Windows Update settings: {ex.Message}"); }

            using HttpClient client = new HttpClient();
            string tempDir = Path.GetTempPath();

            try
            {
                var postFiles = await client.GetFromJsonAsync($"{ServerUrl}/api/files/run_on_finish", AppJsonSerializerContext.Default.ListString);
                if (postFiles != null && postFiles.Any())
                {
                    foreach (var exe in postFiles)
                    {
                        Console.WriteLine($"Downloading post-install script: {exe}...");
                        byte[] fileBytes = await client.GetByteArrayAsync($"{ServerUrl}/static/run_on_finish/{exe}");
                        string tempPath = Path.Combine(tempDir, exe);
                        await File.WriteAllBytesAsync(tempPath, fileBytes);

                        Console.WriteLine($"Executing {exe} and waiting for finish...");
                        using (Process process = Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true }))
                        {
                            process?.WaitForExit();
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Failed to process post-install files: {ex.Message}"); }
        }

        static async Task ProcessApplicationsAsync()
        {
            Console.WriteLine("\n[3] Syncing Application Inventory...");
            using HttpClient client = new HttpClient();
            
            ServerLists response;
            try
            {
                 response = await client.GetFromJsonAsync($"{ServerUrl}/lists", AppJsonSerializerContext.Default.ServerLists);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not connect to server: {ex.Message}");
                return;
            }

            var whitelist = response.whitelist ?? new List<string>();
            var blacklist = response.blacklist ?? new List<string>();

            var installedApps = GetInstalledApps();
            
            // Auto-Uninstall existing blacklisted apps
            foreach (var app in installedApps.Where(a => blacklist.Contains(a.Name)))
            {
                Console.WriteLine($"[!] Silently removing blacklisted app: {app.Name}");
                UninstallAppHeadless(app.UninstallString);
            }

            // Handle unknown apps
            List<string> newWhitelist = new List<string>();
            List<string> newBlacklist = new List<string>();

            var unknownApps = installedApps.Where(a => !whitelist.Contains(a.Name) && !blacklist.Contains(a.Name)).ToList();

            if (unknownApps.Any())
            {
                Console.WriteLine($"\n--- {unknownApps.Count} Unknown Apps Found ---");
                foreach (var app in unknownApps)
                {
                    Console.Write($"App: {app.Name} -> [W]hitelist, [B]lacklist/Uninstall, [S]kip: ");
                    var choice = Console.ReadKey().Key;
                    Console.WriteLine();

                    if (choice == ConsoleKey.W)
                    {
                        newWhitelist.Add(app.Name);
                    }
                    else if (choice == ConsoleKey.B)
                    {
                        newBlacklist.Add(app.Name);
                        Console.WriteLine($"[!] Silently removing: {app.Name}");
                        UninstallAppHeadless(app.UninstallString);
                    }
                }

                if (newWhitelist.Count > 0 || newBlacklist.Count > 0)
                {
                    var payload = new ServerLists { whitelist = newWhitelist, blacklist = newBlacklist };
                    await client.PostAsJsonAsync($"{ServerUrl}/lists", payload, AppJsonSerializerContext.Default.ServerLists);
                    Console.WriteLine("Server lists updated.");
                }
            }
            else
            {
                Console.WriteLine("No unknown apps found. All apps are categorized.");
            }
        }

        static void UninstallAppHeadless(string uninstallString)
        {
            if (string.IsNullOrWhiteSpace(uninstallString)) return;

            try
            {
                string cmd = uninstallString;
                string args = "";

                if (cmd.StartsWith("\""))
                {
                    int nextQuote = cmd.IndexOf("\"", 1);
                    if (nextQuote != -1)
                    {
                        args = cmd.Substring(nextQuote + 1).Trim();
                        cmd = cmd.Substring(1, nextQuote - 1);
                    }
                }
                else
                {
                    int argStart = cmd.IndexOf(" /");
                    if (argStart == -1) argStart = cmd.IndexOf(" -");
                    
                    if (argStart != -1)
                    {
                        args = cmd.Substring(argStart).Trim();
                        cmd = cmd.Substring(0, argStart).Trim();
                    }
                }

                string lowerCmd = cmd.ToLower();
                
                if (lowerCmd.Contains("msiexec"))
                {
                    args = args.Replace("/I", "/X").Replace("/i", "/x"); 
                    if (!args.ToLower().Contains("/quiet") && !args.ToLower().Contains("/qn"))
                    {
                        args += " /qn /norestart";
                    }
                }
                else if (lowerCmd.Contains("unins000") || lowerCmd.Contains("unins001") || lowerCmd.Contains("innosetup"))
                {
                    args += " /VERYSILENT /SUPPRESSMSGBOXES /NORESTART";
                }
                else
                {
                    args += " /S /quiet /norestart"; 
                }

                ProcessStartInfo startInfo = new ProcessStartInfo(cmd, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process process = Process.Start(startInfo))
                {
                    process?.WaitForExit(120000); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Silent uninstall failed: {ex.Message}");
            }
        }

        static List<AppEntry> GetInstalledApps()
        {
            var apps = new List<AppEntry>();
            string[] keys = { 
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", 
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" 
            };

            foreach (var keyPath in keys)
            {
                using RegistryKey rk = Registry.LocalMachine.OpenSubKey(keyPath);
                if (rk == null) continue;
                foreach (string skName in rk.GetSubKeyNames())
                {
                    using RegistryKey sk = rk.OpenSubKey(skName);
                    if (sk == null) continue;
                    
                    string name = sk.GetValue("DisplayName")?.ToString();
                    string uString = sk.GetValue("UninstallString")?.ToString();
                    
                    if (!string.IsNullOrEmpty(name)) 
                    {
                        apps.Add(new AppEntry { Name = name.Trim(), UninstallString = uString });
                    }
                }
            }
            return apps;
        }
    }
}