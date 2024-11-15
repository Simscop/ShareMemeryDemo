using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Windows;

namespace ShareMemeryDemoClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string MapName = @"ShareMemereyDemo";

    private const int MapBufferSize = sizeof(double) * 2 * 5000;

    private const string WriteEventName = @"DemoWriteEvent";

    private const string ReadEventName = @"DemoReadEvent";

    public App()
    {
        var timer = new Timer(AutoShutDownCallBack, null, TimeSpan.FromMinutes(60), Timeout.InfiniteTimeSpan);

        Task.Run(() =>
        {
            var path = FindFileUpwards(Directory.GetCurrentDirectory(), "ShareMemeryDemoServer.exe", 5);

            if (path is null)
                Application.Current.Shutdown();

            var info = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = info;
            process.Start();

            process.OutputDataReceived += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Data);
                Console.ResetColor();
            };

            //process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();
            Console.WriteLine("[SYSTEM] PROCESS EXIT");

            Environment.Exit(0);
        });

        Application.Current.Dispatcher.BeginInvoke(async () =>
        {
            await Task.Delay(1000);

            using var mmf = MemoryMappedFile.CreateOrOpen(MapName, MapBufferSize);
            using var writeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, WriteEventName);
            using var readEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ReadEventName);

            var count = 500000;
            while (count > 0)
            {
                while (writeEvent.WaitOne())
                {
                    using var accessor = mmf.CreateViewAccessor(0, MapBufferSize, MemoryMappedFileAccess.Read);

                    var length = accessor.ReadInt32(0);
                    var index = accessor.ReadInt32(0);

                    var bytes = new byte[length];
                    accessor.ReadArray(2 * sizeof(int), bytes, 0, length);

                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"[CLIENT] [{index}] DONE {DateTime.Now:mm:ss:fff}");
                    Console.ResetColor();

                    var message = Encoding.UTF8.GetString(bytes);
                    readEvent.Set();

                    count--;
                }
            }

            Environment.Exit(0);
        });
    }

    static string? FindFileUpwards(string directory, string targetFileName, int maxDepth)
    {
        for (var i = 0; i < maxDepth; i++)
            directory = Directory.GetParent(directory!)?.FullName!;

        var files = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);

        var name = Path.GetFileNameWithoutExtension(targetFileName);

        return files.FirstOrDefault(item => Path.GetFileNameWithoutExtension(name) == name);
    }

    private void AutoShutDownCallBack(object? state)
    {
        Environment.Exit(0);
    }
}