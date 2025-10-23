using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    public class TCPServer
    {
        private TcpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;

        public TCPServer(string ip = "127.0.0.1", int port = 8888)
        {
            _listener = new TcpListener(IPAddress.Parse(ip), port);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            try
            {
                _listener.Start();
                _isRunning = true;
                
                Console.WriteLine($"TCP эхо-сервер запущен на {_listener.LocalEndpoint}");
                Console.WriteLine("Ожидание подключений...");

                while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync();
                        _ = Task.Run(() => HandleClientAsync(client));
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при принятии подключения: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка запуска сервера: {ex.Message}");
            }
            finally
            {
                Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var clientEndPoint = client.Client.RemoteEndPoint;
            Console.WriteLine($"Подключен клиент: {clientEndPoint}");

            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[1024];
                    
                    while (_isRunning && client.Connected)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        
                        if (bytesRead == 0)
                        {
                            Console.WriteLine($"Клиент {clientEndPoint} отключился");
                            break;
                        }

                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        Console.WriteLine($"Получено от {clientEndPoint}: {message}");

                        string response;
                        
                        if (message.Equals("quit", StringComparison.OrdinalIgnoreCase))
                        {
                            response = "До свидания!";
                            await SendResponseAsync(stream, response);
                            break;
                        }
                        else if (message.Equals("time", StringComparison.OrdinalIgnoreCase))
                        {
                            response = $"Текущее время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                            await SendResponseAsync(stream, response);
                        }
                        else
                        {
                            response = $"Эхо: {message}";
                            await SendResponseAsync(stream, response);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при работе с клиентом {clientEndPoint}: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"Соединение с {clientEndPoint} закрыто");
            }
        }

        private async Task SendResponseAsync(NetworkStream stream, string message)
        {
            var data = Encoding.UTF8.GetBytes(message + Environment.NewLine);
            await stream.WriteAsync(data, 0, data.Length);
        }

        public void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            _listener?.Stop();
            Console.WriteLine("Сервер остановлен");
        }
    }
}