using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    public class TCPClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource;

        public async Task<bool> ConnectAsync(string server = "127.0.0.1", int port = 8888)
        {
            try
            {
                _client = new TcpClient();
                _cancellationTokenSource = new CancellationTokenSource();
                
                await _client.ConnectAsync(server, port);
                _stream = _client.GetStream();
                
                Console.WriteLine($"Подключено к серверу {server}:{port}");
                Console.WriteLine("Введите сообщение (или 'quit' для выхода):");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        public async Task StartAsync()
        {
            if (!await ConnectAsync())
                return;

            // Запускаем задачу для приема сообщений
            var receiveTask = ReceiveMessagesAsync();

            try
            {
                while (_client.Connected && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var message = Console.ReadLine();
                    
                    if (string.IsNullOrEmpty(message))
                        continue;

                    if (message.Equals("quit", StringComparison.OrdinalIgnoreCase))
                    {
                        await SendMessageAsync(message);
                        break;
                    }

                    await SendMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                await DisconnectAsync();
            }
        }

        private async Task SendMessageAsync(string message)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(message + Environment.NewLine);
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024];
            
            try
            {
                while (_client.Connected && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Сервер отключился");
                        break;
                    }

                    var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Сервер: {response.Trim()}");
                }
            }
            catch (Exception)
            {
                // Сокет закрыт
            }
        }

        private async Task DisconnectAsync()
        {
            _cancellationTokenSource.Cancel();
            _stream?.Close();
            _client?.Close();
            Console.WriteLine("Отключено от сервера");
        }
    }
}