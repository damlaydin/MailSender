using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailSender.Services
{
    public class NotificationService : BackgroundService
    {
        private readonly string _hubUrl;
        private HubConnection _connection;

        public NotificationService()
        {
            _hubUrl = "http://localhost:5125/chatHub";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .Build();

            _connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                new ToastContentBuilder()
                    .AddText(user)
                    .AddText(message)
                    .Show(); 
            });

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _connection.StartAsync(stoppingToken);
                    Console.WriteLine("Connected to the SignalR server.");
                    await Task.Delay(Timeout.Infinite, stoppingToken); // Keep the connection alive
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Stopped connecting: {ex.Message}");
                    await Task.Delay(5000, stoppingToken); // Wait before retrying
                }
            }

            await _connection.StopAsync(stoppingToken);
        }
    }
}
