using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Aspire.ElevatorMusic;

public class Program
{
    private static string? _cachedTempFile;

    public static int Main(string[] args)
    {
        // если пользователь явно указал внешний файл – используем его
        var musicFile = args.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(musicFile))
        {
            musicFile = EnsureElevatorTrackOnDisk();
        }

        Console.WriteLine($"[Elevator] Starting elevator music: {musicFile}");

        if (!File.Exists(musicFile))
        {
            Console.WriteLine($"[Elevator] File not found: {musicFile}");
            return 1;
        }

        while (true)
        {
            PlayOnce(musicFile);
        }
    }

    private static string EnsureElevatorTrackOnDisk()
    {
        if (_cachedTempFile is { Length: > 0 } path && File.Exists(path))
        {
            return path;
        }

        var asm = typeof(Program).Assembly;
        var resourceName = asm
            .GetManifestResourceNames()
            .First(n => n.EndsWith("elevator.mp3", StringComparison.OrdinalIgnoreCase));

        using var stream = asm.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException("Embedded elevator.mp3 not found");
        var tempPath = Path.Combine(
            Path.GetTempPath(),
            "aspire-elevator-music-" + Guid.NewGuid().ToString("N") + ".mp3");

        using (var fs = File.Create(tempPath))
        {
            stream.CopyTo(fs);
        }

        _cachedTempFile = tempPath;
        return tempPath;
    }

    private static void PlayOnce(string file)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("afplay", $"\"{file}\"")?.WaitForExit();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var psi = new ProcessStartInfo(
                "powershell",
                $"-NoProfile -Command \"(New-Object Media.SoundPlayer '{file}').PlaySync()\"")
            {
                UseShellExecute = false
            };
            Process.Start(psi)?.WaitForExit();
        }
        else
        {
            Process.Start("ffplay", $"-nodisp -autoexit \"{file}\"")?.WaitForExit();
        }
    }
}