using EVSRS.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EVSRS.Services.BackgroundServices
{
    public class OrderTimeoutService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check mỗi 5 phút

        public OrderTimeoutService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredOrdersAsync();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred while processing expired orders: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait 1 minute on error
                }
            }
        }

        private async Task ProcessExpiredOrdersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var orderBookingService = scope.ServiceProvider.GetRequiredService<IOrderBookingService>();
            
            await orderBookingService.CancelExpiredUnpaidOrdersAsync();
        }
    }
}