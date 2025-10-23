using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    public class SimpleHTTPServer
    {
        private HttpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;

        public SimpleHTTPServer(string prefix = "http://localhost:8080/")
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            try
            {
                _listener.Start();
                _isRunning = true;
                
                Console.WriteLine($"HTTP сервер запущен на {string.Join(", ", _listener.Prefixes)}");
                Console.WriteLine("Ожидание HTTP запросов...");

                while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = Task.Run(() => HandleRequestAsync(context));
                    }
                    catch (HttpListenerException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при обработке запроса: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка запуска HTTP сервера: {ex.Message}");
            }
            finally
            {
                Stop();
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            Console.WriteLine($"HTTP запрос: {request.HttpMethod} {request.Url} от {request.RemoteEndPoint}");

            try
            {
                string responseString;
                string path = request.Url?.AbsolutePath ?? "/";

                switch (path.ToLower())
                {
                    case "/":
                        responseString = CreateHTMLResponse("""
                            <h1>Меню</h1>
                            <p>Доступные страницы:</p>
                            <ul>
                                <li><a href="/time">Текущее время</a></li>
                                <li><a href="/hello">Приветствие</a></li>
                                <li><a href="/echo">Эхо-тест</a></li>
                            </ul>
                            """);
                        break;

                    case "/time":
                        responseString = CreateHTMLResponse($"""
                            <h1>Текущее время</h1>
                            <p>Серверное время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                            <a href="/">На главную</a>
                            """);
                        break;

                    case "/hello":
                        responseString = CreateHTMLResponse("""
                            <h1>Приветствие</h1>
                            <p>Привет от простого HTTP сервера на C#!</p>
                            <a href="/">На главную</a>
                            """);
                        break;

                    case "/echo":
                        responseString = CreateHTMLResponse("""
                            <h1>Эхо-тест</h1>
                            <p>Это тестовая страница для проверки эхо-функциональности</p>
                            <a href="/">На главную</a>
                            """);
                        break;

                    default:
                        responseString = Create404Response();
                        response.StatusCode = 404;
                        break;
                }

                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.ContentEncoding = Encoding.UTF8;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки HTTP запроса: {ex.Message}");
                try
                {
                    response.StatusCode = 500;
                    var errorBuffer = Encoding.UTF8.GetBytes(CreateHTMLResponse($"<h1>Ошибка сервера</h1><p>{ex.Message}</p>"));
                    await response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                    response.OutputStream.Close();
                }
                catch
                {
                    // Игнорируем ошибки при отправке ошибки
                }
            }
        }

        private string CreateHTMLResponse(string content)
        {
            return $"""
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Простой HTTP Сервер</title>
                    <meta charset="utf-8">
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 40px; }}
                        a {{ color: #0066cc; text-decoration: none; }}
                        a:hover {{ text-decoration: underline; }}
                    </style>
                </head>
                <body>
                    {content}
                </body>
                </html>
                """;
        }

        private string Create404Response()
        {
            return CreateHTMLResponse("""
                <h1>404 - Страница не найдена</h1>
                <p>Запрошенная страница не существует.</p>
                <a href="/">На главную</a>
                """);
        }

        public void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            _listener?.Stop();
            _listener?.Close();
            Console.WriteLine("HTTP сервер остановлен");
        }
    }
}
