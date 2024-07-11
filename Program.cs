using Serilog;
using System.Net;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.Text;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Logs;
using AdvancedSharpAdbClient.Models;
using NUDev.ADBSharp;
using Newtonsoft.Json;
using System.Threading.Tasks;

public partial class Config
{
    public ConsoleColor Foregroundcolor { get; set; } = ConsoleColor.Red;
    public ConsoleColor Backgroundcolor { get; set; } = ConsoleColor.Black;
    public string Prefix { get; set; } = "└─";
    public string Arrow { get; set; } = "─> ";
    public string Braces { get; set; } = "()";
    public bool DebugMode { get; set; } = false;
}

public class Program
{
    private const string ConfigFileName = "config.json";
    private Config _config;
    private DeviceData _device = new();
    private AdbClient adbClient = new();

    private async Task InitializeConfigAsync()
    {
        if (!File.Exists(ConfigFileName))
        {
            await WriteDefaultConfigAsync();
        }

        _config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync(ConfigFileName));

        Console.BackgroundColor = _config.Backgroundcolor;
    }

    private async Task WriteDefaultConfigAsync()
    {
        var defaultConfig = new Config();
        var configJson = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
        await File.WriteAllTextAsync(ConfigFileName, configJson);
    }

    private string SubPrompt(string promptMessage = "Title")
    {
        Console.ForegroundColor = _config.Foregroundcolor;
        Console.Write(_config.Prefix);
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(promptMessage);
        Console.ForegroundColor = _config.Foregroundcolor;
        Console.Write(_config.Arrow);
        Console.ForegroundColor = ConsoleColor.White;
        return Console.ReadLine();
    }

    private string Prompt(string promptMessage = "ADBSploit")
    {
        Console.ForegroundColor = _config.Foregroundcolor;
        Console.Write($"{_config.Braces[0]}");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(promptMessage);
        Console.ForegroundColor = _config.Foregroundcolor;
        Console.Write($"{_config.Braces[1]}+{_config.Arrow}");
        Console.ForegroundColor = ConsoleColor.White;
        return Console.ReadLine();
    }

    private async Task BootstrapAsync()
    {
        Console.WriteLine("Bootstrapping packages.");

        Log.Information("Downloading adb and fastboot.");
        using WebClient wc = new();
        var url = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "https://dl.google.com/android/repository/platform-tools-latest-windows.zip"
            : "https://dl.google.com/android/repository/platform-tools-latest-linux.zip";
        await wc.DownloadFileTaskAsync(url, @"platform-tools.zip");

        Log.Information("Extracting tools.");

        ZipFile.ExtractToDirectory(@"platform-tools.zip", @"adb");

        File.Delete(@"platform-tools.zip");

        Console.WriteLine("Bootstrapping done.");
    }
    private Dictionary<string, Action> _commands;

    public Program()
    {
        _commands = new Dictionary<string, Action>
        {
            { "connect", Connect },
            { "select", Select },
            { "help", Help }
        };
    }

    static void Main()
    {
        var args = new Program();

        args.InitializeConfigAsync().Wait();

        if (!Directory.Exists(@"adb"))
        {
            Log.Warning("Directory doesn't exist. Creating directory.");
            Directory.CreateDirectory(@"adb");

            args.BootstrapAsync().Wait();
        }

        string userInput;
        do
        {
            userInput = args._device.Model == "" ? args.Prompt() : args.Prompt(args._device.Model);
            if (args._commands.ContainsKey(userInput))
            {
                //execute action from key im dict.
                Action command = args._commands[userInput];
                command.Invoke();
            }
            else
            {
                Console.WriteLine("Not a command. Type \"help\" to get a list of commands.");
            }
        }
        while (true);
    }

    private void Connect()
    {
        var subInput = SubPrompt("Enter the IP Address of the device.");
        try
        {
            adbClient.Connect(subInput);
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Device/IP address not found.");
        }
    }

    private void Select()
    {
        var subInput = SubPrompt("Enter the number of the device");
        int subInt = int.Parse(subInput) - 1;
        try
        {
            _device = adbClient.GetDevices().ToArray()[subInt];
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Device number is larger and such device doesn't exist. Please use the \"devices\" command to see how many devices are connected.");
        }
    }

    private void Help()
    {
        Log.Information("Displaying help");
        Console.WriteLine("\n\tCommands:\n\n");
        Console.WriteLine("\thelp\t\t\tDisplays this message.");
        Console.WriteLine("\tdevices\t\t\tShows all connected devices.");
        Console.WriteLine("\tversion\t\t\tDisplays the current version of the tool and adb.");
        Console.WriteLine("\texit\t\t\tExits this tool and goes back to the command line.");
        Console.WriteLine("\tclear/cls\t\tClears the terminal.");
        Console.WriteLine("\tselect\t\t\tSelects a device based on the order.");
        Console.WriteLine("\n");
    }

}