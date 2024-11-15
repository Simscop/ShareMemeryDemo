using System;
using System.Configuration;
using System.Data;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Windows;

namespace ShareMemeryDemoServer;

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
        var timer = new Timer(AutoShutDownCallBack, null, TimeSpan.FromSeconds(60), Timeout.InfiniteTimeSpan);

        Application.Current.Dispatcher.BeginInvoke(async () =>
        {
            using var mmf = MemoryMappedFile.CreateOrOpen(MapName, MapBufferSize);
            using var writeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, WriteEventName);
            using var readEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ReadEventName);

            var count = 50000;

            while (count-- > 0)
            {
                await Task.Delay(10);

                using var accessor = mmf.CreateViewAccessor(0, MapBufferSize, MemoryMappedFileAccess.Write);

                var bytes = Encoding.UTF8.GetBytes(new string('h', 7000));

                accessor.Write(0, bytes.Length);
                accessor.Write(0, count);
                accessor.WriteArray(2 * sizeof(int), bytes, 0, bytes.Length);

                Console.WriteLine($"[SERVER] [{count}] WRITE {DateTime.Now:mm:ss:fff}");

                writeEvent.Set();
                readEvent.WaitOne();
            }

            Environment.Exit(0);
        });


    }

    private void AutoShutDownCallBack(object? state)
    {
        Environment.Exit(0);
    }
}