using Serilog;
using System.Net;
using System.Runtime.InteropServices;
using System.IO.Compression;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;

using System.Security.Cryptography;
using AdvancedSharpAdbClient.DeviceCommands;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Sockets;
using System;

DeviceData device = new();

string subprompt(string promptmessage = "Title")
{

    string prefix = "└─";
    string arrow = "─> ";

    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write(prefix);
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write(promptmessage);
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write(arrow);
    Console.ForegroundColor = ConsoleColor.White;
    string input = Console.ReadLine();
    return input;
}

string prompt(string promptmessage = "ADBSploit")
{
    string prefix = "(";
    string arrow = ")─> ";

    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write(prefix);
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write(promptmessage);
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write(arrow);
    Console.ForegroundColor = ConsoleColor.White;
    string input = Console.ReadLine();
    return input;
}

void bootstrap()
{
    Console.WriteLine("Bootstrapping packages.");

    Log.Information("Downloading adb and fastboot.");
    WebClient wc = new();
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        wc.DownloadFile("https://dl.google.com/android/repository/platform-tools-latest-windows.zip", @"platform-tools.zip");
    }
    else
    {
        wc.DownloadFile("https://dl.google.com/android/repository/platform-tools-latest-linux.zip", @"platform-tools.zip");
    }

    Log.Information("Extracting tools.");

    ZipFile.ExtractToDirectory(@"platform-tools.zip", @"adb");

    Console.WriteLine("Bootstrapping done.");
}

string ver = "0.1";

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("Starting ADBSploit...");
Console.WriteLine("ADBSploit. Version: {0}, OS: {1}, Platform: {2}", ver, RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture);
Log.Information("Checking for adb directory");

if (!Directory.Exists(@"adb"))
{
    Log.Warning("Directory doesn't exist. Creating directory.");
    Directory.CreateDirectory(@"adb");

    bootstrap();
}

Console.WriteLine("Starting up the ADB Client.");

if (!AdbServer.Instance.GetStatus().IsRunning)
{
    AdbServer server = new AdbServer();
    StartServerResult result = server.StartServer(@"adb\platform-tools\adb.exe", false);
    if (result != StartServerResult.Started)
    {
        Console.WriteLine("Can't start adb server");
    }
}

Console.WriteLine("ADB is up.");

AdbClient adbClient;

adbClient = new AdbClient();
adbClient.Connect("127.0.0.1:62001");


string userinp;
string subinp;
do
{
    Program program = new();
    if (device.Model == "")
    {
        userinp = prompt();
    }
    else
    {
        userinp = prompt(device.Model);
    }
    switch (userinp)
    {
        default: Console.WriteLine("Not a command. Type \"help\" to get a list of commands."); break;
        case "connect":
            subinp = subprompt("Enter the IP Address of the device.");
            try
            {
                adbClient.Connect(subinp);
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Device/IP address not found.");
            }
            break;
        case "select":
            subinp = subprompt("Enter the number of the device");
            int subint = int.Parse(subinp) - 1;
            try
            {
                device = adbClient.GetDevices().ToArray()[subint];
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Device number is larger and such device doesn't exist. Please use the \"devices\" command to see how many devices are connected.");
                break;
            }
            break;
        case "reboot":
            if (device.Model == "")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No device selected.");
                break;
            }
            adbClient.Reboot(device);
            break;
        case "exit":
            Console.WriteLine("Exiting.");
            Log.Warning("Flushing everything and exiting");
            Log.CloseAndFlush();
            adbClient.KillAdb();
            Environment.Exit(0);
            break;
        case "debug":
            subinp = subprompt("Enter debug subprompt");
            Console.WriteLine("Entered text was: {0}", subinp);
            break;
        case "devices":
            Log.Information("Getting Devices");
            adbClient.GetDevices().ToList().ForEach(device =>
            {
                Console.WriteLine(device.Model);
                Log.Information("Devices found: {0}", device.Model);
            });
            break;
        case "help":
            Log.Information("Displaying help");
            Console.WriteLine("\n\tCommands:\n\n");
            Console.WriteLine("\thelp\t\t\tDisplays this message.");
            Console.WriteLine("\tdevices\t\t\tShows all connected devices.");
            Console.WriteLine("\tversion\t\t\tDisplays the current version of the tool and adb.");
            Console.WriteLine("\texit\t\t\tExits this tool and goes back to the command line.");
            Console.WriteLine("\tclear/cls\t\tClears the terminal.");
            Console.WriteLine("\tselect\t\t\tSelects a device based on the order.");
            Console.WriteLine("\n");
            break;
        case "version":
            Log.Information("Displaying version");
            Console.WriteLine("Adbsploit version: {0}\nAdb version: {1}", ver, adbClient.GetAdbVersion());
            break;
        case "cls":
            Log.Information("Clearing Console");
            Console.Clear();
            break;
        case "clear":
            Log.Information("Clearing Console");
            Console.Clear();
            break;
        case "":
            break;
    }
}
while (true);