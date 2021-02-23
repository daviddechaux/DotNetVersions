using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace DotNetVersions
{
    class Program
    {
        static void Main(string[] args)
        {
            bool batchMode = false;

            if (IsHelpCommand(args))
            {
                Console.WriteLine("Writes all the currently installed versions of .NET Framework platform in the system.");
                Console.WriteLine("Use --b, -b or /b to use in a batch, showing only the installed versions, without any extra informational lines.");
            }
            else
            {
                if (args.Length > 0 && HasParameter(args[0].ToLower(), "b"))
                    batchMode = true;

                GetNetCoreVersion();
                Get1To45VersionFromRegistry();
                Get45PlusFromRegistry();
            }

            if (!batchMode)
                Console.ReadKey();
        }

        private static bool IsHelpCommand(string[] args)
        {
            return args.Length > 0 && (HasParameter(args[0], "help") || HasParameter(args[0], "?"));
        }

        private static bool HasParameter(string parameter, string testParameter)
        {
            parameter = parameter.ToLower();
            testParameter = testParameter.ToLower();

            string[] prefixParemeter = new string[] { "/", "-", "--" };
            foreach (var prefix in prefixParemeter)
            {
                if (parameter == $"{prefix}{testParameter}")
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Writes the version
        /// </summary>
        /// <param name="version"></param>
        /// <param name="spLevel"></param>
        private static void WriteVersion(string version, string spLevel = "")
        {
            version = version.Trim();
            if (string.IsNullOrEmpty(version))
                return;

            string spLevelString = "";
            if (!string.IsNullOrEmpty(spLevel))
                spLevelString = " Service Pack " + spLevel;

            Console.WriteLine($"{version}{spLevelString}");
        }

        /// <summary>
        /// Check .net framework for version below 4.5
        /// </summary>
        private static void Get1To45VersionFromRegistry()
        {
            // Opens the registry key for the .NET Framework entry.
            using (RegistryKey ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
            {
                foreach (string versionKeyName in ndpKey.GetSubKeyNames())
                {
                    // Skip .NET Framework 4.5 version information.
                    if (versionKeyName == "v4")
                        continue;

                    if (versionKeyName.StartsWith("v"))
                    {
                        RegistryKey versionKey = ndpKey.OpenSubKey(versionKeyName);

                        string name = GetNameFromVersion(versionKey);
                        string sp = GetSPName(versionKey);
                        string install = GetInstall(versionKey);

                        if (string.IsNullOrEmpty(install)) // No install info; it must be in a child subkey.
                            WriteVersion(name);
                        else if (!(string.IsNullOrEmpty(sp)) && install == "1")
                            WriteVersion(name, sp);

                        if (!string.IsNullOrEmpty(name))
                            continue;

                        foreach (string subKeyName in versionKey.GetSubKeyNames())
                        {
                            RegistryKey subKey = versionKey.OpenSubKey(subKeyName);
                            name = GetNameFromVersion(subKey);
                            if (!string.IsNullOrEmpty(name))
                                sp = GetSPName(subKey);

                            install = GetInstall(subKey);
                            if (string.IsNullOrEmpty(install))
                            {
                                //No install info; it must be later.
                                WriteVersion(name);
                            }
                            else
                            {
                                if (!(string.IsNullOrEmpty(sp)) && install == "1")
                                    WriteVersion(name, sp);
                                else if (install == "1")
                                    WriteVersion(name);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the installation flag, or an empty string if there is none.
        /// </summary>
        /// <param name="subKey"></param>
        /// <returns></returns>
        private static string GetInstall(RegistryKey subKey)
        {
            return subKey.GetValue("Install", "").ToString();
        }

        /// <summary>
        /// Get the service pack (SP) number.
        /// </summary>
        /// <param name="subKey"></param>
        /// <returns></returns>
        private static string GetSPName(RegistryKey subKey)
        {
            return subKey.GetValue("SP", "").ToString();
        }

        /// <summary>
        /// Get the .NET Framework version value.
        /// </summary>
        /// <param name="subKey"></param>
        /// <returns></returns>
        private static string GetNameFromVersion(RegistryKey subKey)
        {
            return (string)subKey.GetValue("Version", "");
        }

        /// <summary>
        /// Check .net framework for version 4.5 and upper
        /// </summary>
        private static void Get45PlusFromRegistry()
        {
            using (RegistryKey ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
            {
                if (ndpKey == null)
                    return;

                //First check if there's an specific version indicated
                if (ndpKey.GetValue("Version") != null)
                {
                    WriteVersion(ndpKey.GetValue("Version").ToString());
                }
                else if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    WriteVersion(CheckFor45PlusVersion((int)ndpKey.GetValue("Release")));
                }
            }
        }

        /// <summary>
        /// Checking the version using >= enables forward compatibility.
        /// </summary>
        /// <param name="releaseKey"></param>
        /// <returns></returns>
        private static string CheckFor45PlusVersion(int releaseKey)
        {
            switch (releaseKey)
            {
                case 528040: return "4.8";
                case 461808: return "4.7.2";
                case 461308: return "4.7.1";
                case 460798: return "4.7";
                case 394802: return "4.6.2";
                case 394254: return "4.6.1";
                case 393295: return "4.6";
                case 379893: return "4.5.2";
                case 378675: return "4.5.1";
                case 378389: return "4.5";
                // This code should never execute. A non-null release key should mean
                // that 4.5 or later is installed.
                default: return "";
            }
        }

        private static void GetNetCoreVersion()
        {
            ExecuteCommand(new string[] { "dotnet --list-runtimes", "dotnet --version", "dotnet --list-sdks", });
        }

        private static void ExecuteCommand(string[] cmds)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            foreach (string cmd in cmds)
            {
                process.StandardInput.WriteLine(cmd);
            }

            process.StandardInput.Flush();
            process.StandardInput.Close();
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
        }
    }
}