using System;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    class Program
    {
        private static TCPServer _tcpServer;
        private static SimpleHTTPServer _httpServer;
        private static readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(false);

        static async Task Main(string[] args)
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\nПолучен сигнал завершения...");
                _shutdownEvent.Set();
            };
        
            _tcpServer = new TCPServer();
            var tcpTask = Task.Run(() => _tcpServer.StartAsync());

            _httpServer = new SimpleHTTPServer();
            var httpTask = Task.Run(() => _httpServer.StartAsync());

            await Task.Delay(1000);

            Console.WriteLine("\nСерверы запущены!");
            Console.WriteLine("1. TCP эхо-сервер: 127.0.0.1:8888");
            Console.WriteLine("2. HTTP сервер: http://localhost:8080");
            Console.WriteLine("3. Для остановки нажмите Ctrl+C\n");

            if (args.Length > 0 && args[0] == "client")
            {
                await RunTCPClientDemo();
            }
            else
            {
                _ = Task.Run(RunTCPClientDemo);
                
                Console.WriteLine("Ожидание запросов...");
                _shutdownEvent.WaitOne();
            }

            Console.WriteLine("Завершение работы серверов...");
            _tcpServer.Stop();
            _httpServer.Stop();

            await Task.WhenAll(tcpTask, httpTask);
            Console.WriteLine("Все серверы остановлены.");
        }

        private static async Task RunTCPClientDemo()
        {
            await Task.Delay(2000);
            
            Console.WriteLine("\n--- Демонстрация TCP клиента ---");
            
            var client = new TCPClient();
            if (await client.ConnectAsync())
            {
                await client.StartAsync();
                Console.WriteLine("--- Демонстрация завершена ---\n");
            }
        }
    }
}
