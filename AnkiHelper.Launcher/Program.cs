using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

const string frontendUrl = "http://localhost:56573";

var repoRoot = FindRepoRoot();
var backendProject = Path.Combine(repoRoot, "AnkiHelper", "AnkiHelper.csproj");
var frontendDirectory = Path.Combine(repoRoot, "ankihelperclient");
var viteScriptPath = Path.Combine(frontendDirectory, "node_modules", "vite", "bin", "vite.js");

if (!File.Exists(backendProject))
{
    Console.WriteLine($"Backend project was not found: {backendProject}");
    return 1;
}

if (!Directory.Exists(frontendDirectory))
{
    Console.WriteLine($"Frontend directory was not found: {frontendDirectory}");
    return 1;
}

if (!File.Exists(viteScriptPath))
{
    Console.WriteLine("Frontend dependencies were not found. Run `npm install` in the ankihelperclient folder first.");
    return 1;
}

Console.Title = "AnkiHelper Launcher";
Console.WriteLine("Starting AnkiHelper...");
Console.WriteLine();

var jobHandle = OperatingSystem.IsWindows()
    ? WindowsJobObject.CreateKillOnCloseJob()
    : IntPtr.Zero;

var frontendReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

Process? backend = null;
Process? frontend = null;
var isStopping = false;

try
{
    backend = StartProcess(
    "api",
    "dotnet",
    $"run --project \"{backendProject}\" --launch-profile https",
    repoRoot,
    jobHandle: jobHandle);

    frontend = StartProcess(
    "client",
    "node",
    $"\"{viteScriptPath}\" --host localhost --clearScreen false",
    frontendDirectory,
    environment =>
    {
        environment["NO_COLOR"] = "1";
        environment["VITE_USE_HTTPS"] = "false";
    },
    line =>
    {
        var cleanLine = StripAnsi(line);

        if (cleanLine.Contains("Local", StringComparison.OrdinalIgnoreCase) &&
            cleanLine.Contains("56573", StringComparison.OrdinalIgnoreCase))
        {
            frontendReady.TrySetResult();
        }
    },
    jobHandle);

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        Stop();
    };

    _ = Task.Run(async () =>
    {
        try
        {
            await frontendReady.Task.WaitAsync(TimeSpan.FromSeconds(45));
        }
        catch (TimeoutException)
        {
            Console.WriteLine("Frontend readiness was not detected in time. Opening the browser anyway.");
        }

        OpenBrowser(frontendUrl);
    });

    Console.WriteLine();
    Console.WriteLine($"AnkiHelper is starting at {frontendUrl}");
    Console.WriteLine("The backend API still runs on HTTPS. The launcher opens the local frontend over HTTP to avoid browser certificate warnings.");
    Console.WriteLine("Press Enter to stop both server and client.");
    Console.ReadLine();

    Stop();
    return 0;
}
finally
{
    Stop();

    if (jobHandle != IntPtr.Zero)
        WindowsJobObject.CloseHandle(jobHandle);
}

void Stop()
{
    if (isStopping)
        return;

    isStopping = true;
    Console.WriteLine();
    Console.WriteLine("Stopping AnkiHelper...");

    StopProcess(frontend);
    StopProcess(backend);
}

static Process StartProcess(
    string name,
    string fileName,
    string arguments,
    string workingDirectory,
    Action<IDictionary<string, string?>>? configureEnvironment = null,
    Action<string>? onOutputLine = null,
    IntPtr jobHandle = default)
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        },
        EnableRaisingEvents = true
    };

    configureEnvironment?.Invoke(process.StartInfo.Environment);

    process.OutputDataReceived += (_, args) => WriteProcessLine(name, args.Data, onOutputLine);
    process.ErrorDataReceived += (_, args) => WriteProcessLine(name, args.Data, onOutputLine);

    process.Start();

    if (jobHandle != IntPtr.Zero && OperatingSystem.IsWindows())
        WindowsJobObject.AssignProcess(jobHandle, process.Handle);

    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    return process;
}

static void StopProcess(Process? process)
{
    try
    {
        if (process is not null && !process.HasExited)
            process.Kill(entireProcessTree: true);
    }
    catch (InvalidOperationException)
    {
    }
}

static void WriteProcessLine(string name, string? line, Action<string>? onOutputLine)
{
    if (string.IsNullOrWhiteSpace(line))
        return;

    Console.WriteLine($"[{name}] {line}");
    onOutputLine?.Invoke(line);
}

static string StripAnsi(string text)
{
    return Regex.Replace(text, @"\u001b\[[0-9;]*m", string.Empty);
}

static string FindRepoRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "AnkiHelper.slnx")))
            return directory.FullName;

        directory = directory.Parent;
    }

    return Directory.GetCurrentDirectory();
}

static void OpenBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Could not open browser automatically: {exception.Message}");
    }
}

internal static class WindowsJobObject
{
    private const int JobObjectExtendedLimitInformation = 9;
    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

    public static IntPtr CreateKillOnCloseJob()
    {
        var handle = CreateJobObject(IntPtr.Zero, null);

        if (handle == IntPtr.Zero)
            return IntPtr.Zero;

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        var length = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        var pointer = Marshal.AllocHGlobal(length);

        try
        {
            Marshal.StructureToPtr(info, pointer, false);

            if (!SetInformationJobObject(handle, JobObjectExtendedLimitInformation, pointer, (uint)length))
            {
                CloseHandle(handle);
                return IntPtr.Zero;
            }

            return handle;
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    public static bool AssignProcess(IntPtr jobHandle, IntPtr processHandle)
    {
        return AssignProcessToJobObject(jobHandle, processHandle);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll")]
    private static extern bool SetInformationJobObject(
        IntPtr hJob,
        int jobObjectInfoClass,
        IntPtr lpJobObjectInfo,
        uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll")]
    private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public long Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }
}
