using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Hosting;
using System.Xml.Linq;
using Dnn.Mcp.WebApi;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Installer.Packages;
using DotNetNuke.Services.Log.EventLog;
using Newtonsoft.Json;

namespace Satrabel.AIChat.Tools
{
    /// <summary>
    /// Read-only diagnostics for testing a DNN upgrade: installed extension versions, the
    /// Admin Log, the application log files, and web.config binding redirects.
    /// </summary>
    /// <remarks>
    /// Installing packages is deliberately not here. That goes through Bulk Install's own
    /// remote API, so the MCP key never carries install rights. Every tool is Host-only
    /// because each exposes server-wide state, not just the current portal.
    /// </remarks>
    public class UpgradeTools : IMcpProvider
    {
        private const string LogFolder = "~/Portals/_default/Logs";
        private const int MaxOutputChars = 200000;

        private static readonly XNamespace AsmV1 = "urn:schemas-microsoft-com:asm.v1";

        /// <inheritdoc />
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "get-extensions",
                Title = "Get installed extensions",
                Description =
                    "List installed extension packages with their versions, as Persona Bar > "
                    + "Extensions shows them. Use before and after an upgrade to diff versions. "
                    + "Host only.",
                Category = "System",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    ToolKit.Param(
                        "includeSystem",
                        "bool",
                        "Include DNN's own system packages. Defaults to false.",
                        false),
                    ToolKit.Param(
                        "packageType",
                        "string",
                        "Only this package type, e.g. Module, Skin, Library, Provider.",
                        false),
                    ToolKit.Param(
                        "name",
                        "string",
                        "Only packages whose Name or FriendlyName contains this text.",
                        false),
                },
                Handler = ToolKit.Guard("get-extensions", GetExtensions),
            });

            registry.RegisterTool(new ToolDefinition
            {
                Name = "get-admin-log",
                Title = "Get Admin Log",
                Description =
                    "Read the DNN Admin Log (event log), newest first, including exception "
                    + "details. Host only.",
                Category = "System",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    ToolKit.Param(
                        "logType",
                        "string",
                        "Log type key, e.g. GENERAL_EXCEPTION, MODULE_UPDATED, HOST_ALERT. "
                        + "Omit for all types.",
                        false),
                    ToolKit.Param(
                        "portalId",
                        "int",
                        "Only entries for this portal. Omit for all portals.",
                        false),
                    ToolKit.Param("pageSize", "int", "Entries per page, 1-200. Defaults to 25.", false),
                    ToolKit.Param("pageIndex", "int", "Zero-based page. Defaults to 0.", false),
                    ToolKit.Param(
                        "since",
                        "string",
                        "Only entries on or after this server-local date/time, e.g. "
                        + "2026-10-28T11:30. Applied to the returned page.",
                        false),
                },
                Handler = ToolKit.Guard("get-admin-log", GetAdminLog),
            });

            registry.RegisterTool(new ToolDefinition
            {
                Name = "read-log-file",
                Title = "Read application log file",
                Description =
                    "List or tail the application log files in Portals/_default/Logs (Serilog "
                    + "log<yyyyMMdd>.resources on DNN 10.4+, log4net *.log.resources before). "
                    + "Host only.",
                Category = "System",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    ToolKit.Param(
                        "file",
                        "string",
                        "File name from the list. Omit to list the files; pass 'latest' for the "
                        + "most recently written one.",
                        false),
                    ToolKit.Param(
                        "tail",
                        "int",
                        "Return the last N matching lines, 1-2000. Defaults to 200.",
                        false),
                    ToolKit.Param(
                        "contains",
                        "string",
                        "Only lines containing this text (case-insensitive), e.g. [ERR] or ERROR.",
                        false),
                },
                Handler = ToolKit.Guard("read-log-file", ReadLogFile),
            });

            registry.RegisterTool(new ToolDefinition
            {
                Name = "check-binding-redirects",
                Title = "Check binding redirects",
                Description =
                    "Compare web.config assembly binding redirects with the assembly versions "
                    + "actually in bin. A redirect to a different version than bin holds makes "
                    + "that assembly fail to load. Pass 'proposed' to check the redirects a "
                    + "package manifest would write before installing it. Host only.",
                Category = "System",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    ToolKit.Param(
                        "proposed",
                        "string",
                        "Comma-separated Name=Version pairs a package would redirect to, e.g. "
                        + "MailKit=4.15.0.0,MimeKit=4.15.0.0. Omit to check web.config only.",
                        false),
                    ToolKit.Param(
                        "onlyProblems",
                        "bool",
                        "Return only mismatches. Defaults to false.",
                        false),
                },
                Handler = ToolKit.Guard("check-binding-redirects", CheckBindingRedirects),
            });
        }

        private static CallToolResult? HostOnly()
        {
            var user = UserController.Instance.GetCurrentUserInfo();

            return user != null && user.IsSuperUser
                ? null
                : ToolKit.Fail("Host (superuser) only. The API key's user is not a Host.");
        }

        private static CallToolResult GetExtensions(Dictionary<string, object> args)
        {
            var denied = HostOnly();
            if (denied != null)
            {
                return denied;
            }

            var includeSystem = ToolKit.Bool(args, "includeSystem");
            var packageType = ToolKit.Str(args, "packageType");
            var name = ToolKit.Str(args, "name");

            var packages = PackageController.Instance.GetExtensionPackages(Null.NullInteger)
                .Where(p => includeSystem || !p.IsSystemPackage)
                .Where(p => packageType == null
                    || string.Equals(p.PackageType, packageType, StringComparison.OrdinalIgnoreCase))
                .Where(p => name == null
                    || p.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0
                    || (p.FriendlyName ?? string.Empty).IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(p => p.PackageType)
                .ThenBy(p => p.Name)
                .Select(p => new
                {
                    p.Name,
                    p.FriendlyName,
                    p.PackageType,
                    Version = p.Version?.ToString(),
                    p.IsSystemPackage,
                    p.Owner,
                    p.Organization,
                    p.LastModifiedOnDate,
                })
                .ToList();

            return ToolKit.Ok(JsonConvert.SerializeObject(new { count = packages.Count, packages }));
        }

        private static CallToolResult GetAdminLog(Dictionary<string, object> args)
        {
            var denied = HostOnly();
            if (denied != null)
            {
                return denied;
            }

            var logType = ToolKit.Str(args, "logType") ?? string.Empty;
            var portalId = ToolKit.TryInt(args, "portalId", out var p) ? p : Null.NullInteger;
            var pageSize = ToolKit.TryInt(args, "pageSize", out var size) ? Math.Max(1, Math.Min(200, size)) : 25;
            var pageIndex = ToolKit.TryInt(args, "pageIndex", out var index) ? Math.Max(0, index) : 0;

            DateTime? since = null;
            var sinceText = ToolKit.Str(args, "since");
            if (sinceText != null)
            {
                if (!DateTime.TryParse(sinceText, out var parsed))
                {
                    return ToolKit.Fail("since must be a date/time, e.g. 2026-10-28T11:30.");
                }

                since = parsed;
            }

            var total = 0;
            var logs = LogController.Instance.GetLogs(portalId, logType, pageSize, pageIndex, ref total);

            var entries = logs
                .Where(l => since == null || l.LogCreateDate >= since)
                .Select(l => new
                {
                    l.LogCreateDate,
                    l.LogTypeKey,
                    l.LogPortalName,
                    l.LogUserName,
                    l.LogServerName,
                    Summary = l.LogProperties?.Summary,
                    Exception = l.Exception == null || string.IsNullOrEmpty(l.Exception.Message)
                        ? null
                        : new
                        {
                            l.Exception.Message,
                            l.Exception.Source,
                            l.Exception.RawUrl,
                            StackTrace = Truncate(l.Exception.StackTrace, 2000),
                            l.Exception.InnerMessage,
                        },
                })
                .ToList();

            return ToolKit.Ok(JsonConvert.SerializeObject(new
            {
                totalRecords = total,
                pageIndex,
                pageSize,
                returned = entries.Count,
                entries,
            }));
        }

        private static CallToolResult ReadLogFile(Dictionary<string, object> args)
        {
            var denied = HostOnly();
            if (denied != null)
            {
                return denied;
            }

            var folder = new DirectoryInfo(HostingEnvironment.MapPath(LogFolder));
            if (!folder.Exists)
            {
                return ToolKit.Fail("Log folder not found: " + LogFolder);
            }

            var files = folder.GetFiles().OrderByDescending(f => f.LastWriteTimeUtc).ToList();
            var requested = ToolKit.Str(args, "file");

            if (requested == null)
            {
                return ToolKit.Ok(JsonConvert.SerializeObject(files.Select(f => new
                {
                    f.Name,
                    SizeKb = Math.Round(f.Length / 1024.0, 1),
                    f.LastWriteTime,
                })));
            }

            // Match by name only, so a path cannot reach outside the log folder.
            var file = string.Equals(requested, "latest", StringComparison.OrdinalIgnoreCase)
                ? files.FirstOrDefault()
                : files.FirstOrDefault(f => string.Equals(f.Name, requested, StringComparison.OrdinalIgnoreCase));
            if (file == null)
            {
                return ToolKit.Fail("No log file named '" + requested + "'. Omit 'file' to list them.");
            }

            var tail = ToolKit.TryInt(args, "tail", out var t) ? Math.Max(1, Math.Min(2000, t)) : 200;
            var contains = ToolKit.Str(args, "contains");

            // The logger keeps the file open for writing, so it has to be shared.
            var lines = new Queue<string>(tail);
            var matched = 0;
            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (contains != null && line.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    matched++;
                    if (lines.Count == tail)
                    {
                        lines.Dequeue();
                    }

                    lines.Enqueue(line);
                }
            }

            var text = new StringBuilder()
                .AppendLine($"{file.Name}: {matched} matching line(s), last {lines.Count} shown.")
                .Append(string.Join(Environment.NewLine, lines))
                .ToString();

            return ToolKit.Ok(Truncate(text, MaxOutputChars) ?? string.Empty);
        }

        private static CallToolResult CheckBindingRedirects(Dictionary<string, object> args)
        {
            var denied = HostOnly();
            if (denied != null)
            {
                return denied;
            }

            var onlyProblems = ToolKit.Bool(args, "onlyProblems");
            var proposed = ParseProposed(ToolKit.Str(args, "proposed"), out var parseError);
            if (parseError != null)
            {
                return ToolKit.Fail(parseError);
            }

            var bin = HostingEnvironment.MapPath("~/bin");
            var config = XDocument.Load(HostingEnvironment.MapPath("~/web.config"));

            var redirects = config.Descendants(AsmV1 + "dependentAssembly")
                .Select(d => new
                {
                    Name = (string)d.Element(AsmV1 + "assemblyIdentity")?.Attribute("name"),
                    Redirect = Normalize((string)d.Element(AsmV1 + "bindingRedirect")?.Attribute("newVersion")),
                })
                .Where(r => r.Name != null)
                .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Redirect, StringComparer.OrdinalIgnoreCase);

            var names = redirects.Keys.Union(proposed.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(n => n);

            var results = names.Select(n =>
            {
                redirects.TryGetValue(n, out var current);
                proposed.TryGetValue(n, out var next);
                var binVersion = BinVersion(bin, n);

                return new
                {
                    Assembly = n,
                    BinVersion = binVersion,
                    Redirect = current,
                    Proposed = next,
                    Status = Status(binVersion, current, next),
                };
            })
            .Where(r => !onlyProblems || r.Status != "ok" && r.Status != "not in bin")
            .ToList();

            return ToolKit.Ok(JsonConvert.SerializeObject(new
            {
                checkedCount = names.Count(),
                problems = results.Count(r => r.Status != "ok" && r.Status != "not in bin"),
                results,
            }));
        }

        private static string Status(string? binVersion, string? redirect, string? proposed)
        {
            if (binVersion == null)
            {
                // GAC or framework assemblies; nothing in bin to compare against.
                return "not in bin";
            }

            if (proposed != null && proposed != binVersion)
            {
                return "proposed redirect differs from bin";
            }

            if (redirect != null && redirect != binVersion)
            {
                return "redirect differs from bin";
            }

            return "ok";
        }

        private static string? BinVersion(string bin, string assemblyName)
        {
            var path = Path.Combine(bin, assemblyName + ".dll");

            return File.Exists(path) ? AssemblyName.GetAssemblyName(path).Version.ToString() : null;
        }

        private static Dictionary<string, string> ParseProposed(string? text, out string? error)
        {
            error = null;
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (text == null)
            {
                return result;
            }

            foreach (var pair in text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=');
                if (parts.Length != 2 || !Version.TryParse(parts[1].Trim(), out var version))
                {
                    error = "proposed must be Name=Version pairs, e.g. MailKit=4.15.0.0. Bad pair: '" + pair.Trim() + "'.";
                    return result;
                }

                result[parts[0].Trim()] = Normalize(version);
            }

            return result;
        }

        /// <summary>Pads a version to four parts so "4.15" and "4.15.0.0" compare equal.</summary>
        private static string? Normalize(string? version)
        {
            return Version.TryParse(version, out var v) ? Normalize(v) : version;
        }

        private static string Normalize(Version v)
        {
            return new Version(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0)).ToString();
        }

        private static string? Truncate(string? text, int max)
        {
            return text == null || text.Length <= max ? text : text.Substring(0, max) + "… [truncated]";
        }
    }
}
