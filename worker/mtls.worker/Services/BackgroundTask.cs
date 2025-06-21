namespace mtls.worker.Services;

public class BackgroundTask : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public BackgroundTask(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine("Making request to broker...");
                var client = _httpClientFactory.CreateClient("workerClient");
                var response = await client.GetAsync("/test", stoppingToken);
                 Console.WriteLine("Making request to broker: {0}", client.BaseAddress);
                 Console.WriteLine($"response: {response}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Request successful: {0}", response.StatusCode);
                }
                else
                {
                    Console.WriteLine("Request failed: {0}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(10000), stoppingToken);
        }
    }
}