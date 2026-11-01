using System.Diagnostics;
using MQTTnet.Protocol;
using Simulator;

// See https://aka.ms/new-console-template for more information
//
//
//
//
//
int deviceCount = 1;
int interval = 10;
string add = "localhost";

if (args.Length > 0 && int.TryParse(args[0], out int parsedCount))
{
    deviceCount = parsedCount;
}
if (args.Length > 1 && int.TryParse(args[1], out int parsedInterval))
{
    interval = parsedInterval;
}
if (args.Length > 2)
{
    add = args[2];
}
var ct = new CancellationTokenSource();
var list = new List<Task>();
var deviceList = new List<VirtualDevice>();
var cts = ct.Token;
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    ct.Cancel();
};
DateTime startTimeUtc = DateTime.UtcNow;
Stopwatch sw3 = Stopwatch.StartNew();
try
{
    for (int i = 0; i < deviceCount; i++)
    {
        var device = new VirtualDevice($"sim-{(i + 1):D5}");
        deviceList.Add(device);
        list.Add(device.Publish_Sim_Message(interval, add, ct.Token));
        await Task.Delay(200, cts);
    }
}
catch (TaskCanceledException)
{
    Console.WriteLine("Canceled");
}
try
{
    await Task.WhenAll(list);
}
catch (Exception ex)
{
    Console.WriteLine($"{ex}");
}
sw3.Stop();
DateTime endTimeUtc = DateTime.UtcNow;
int totalSent = deviceList.Sum(d => d.Sent);
int totalFailed = deviceList.Sum(d => d.Failed);
int totalConnected = deviceList.Count(d => d.Connected);

double totalSeconds = sw3.Elapsed.TotalSeconds;
double messagePerSec = totalSeconds > 0 ? totalSent / totalSeconds : 0;

List<double> allTimes = deviceList.SelectMany(d => d.Times).ToList();
double avgTime = 0;
double maxTime = 0;
double p95Time = 0;

if (allTimes.Any())
{
    avgTime = allTimes.Average();
    maxTime = allTimes.Max();

    var sortedTimes = allTimes.OrderBy(t => t).ToList();
    int index95 = (int)Math.Ceiling(sortedTimes.Count * 0.95) - 1;
    if (index95 >= 0 && index95 < sortedTimes.Count())
    {
        p95Time = sortedTimes[index95];
    }
}
Console.WriteLine("\n statistics");
Console.WriteLine(
    $"run time: {startTimeUtc:yyyy-MM-dd HH:mm:ss.fff} to {endTimeUtc:yyyy-MM-dd HH:mm:ss.fff}"
);
Console.WriteLine($"{totalSeconds:F2} s");
Console.WriteLine($"Devices: Started: {deviceList.Count} | Connected {totalConnected}");
Console.WriteLine($"Messages: Sent: {totalSent}| Failed :{totalFailed}");
Console.WriteLine($"Throughput:{messagePerSec:F2} msg/s");
Console.WriteLine($"avgTime: {avgTime:F2}, p95Time: {p95Time:F2} , maxTime: {maxTime:F2} ms");
Console.WriteLine($"measuremants: {totalSent * 3}");
