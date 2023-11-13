using System.Diagnostics;

namespace ChatRPGTests;

public class ChatRPGFixture : IAsyncLifetime
{
    private Process? _appProcess;

    public async Task InitializeAsync()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project ../../../../ChatRPG/ChatRPG.csproj",
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _appProcess = new Process { StartInfo = startInfo };
        _appProcess.Start();
        await WaitForAppToStart();
    }

    public async Task DisposeAsync()
    {
        if (!_appProcess!.HasExited)
        {
            _appProcess.Kill();
            await _appProcess.WaitForExitAsync();
        }

        _appProcess.Dispose();
    }

    private async Task WaitForAppToStart()
    {
        int maxAttempts = 10;
        TimeSpan delayBetweenAttempts = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                // Make a request to a known endpoint
                using HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = await httpClient.GetAsync("http://localhost:5111/");
                if (response.IsSuccessStatusCode)
                {
                    return; // App is ready
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request failed: {ex.Message}");
            }

            Task.Delay(delayBetweenAttempts).Wait();
        }

        throw new InvalidOperationException("Failed to determine if the app is ready.");
    }
}
