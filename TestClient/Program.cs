using Newtonsoft.Json;
using System;
using WebSocketSharp;

namespace TestClient
{
    class Program
    {
        private static WebSocket _socket;
        static void Main(string[] args)
        {
            var url = "ws://127.0.0.1:8080";
            Console.WriteLine(url);
            _socket = new WebSocket(url);
            _socket.OnMessage += Socket_OnMessage;

            Console.WriteLine("Press Any Key to Send Request. Or Esc to Exit");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
                Send();
            }

            SendStop();
            Console.ReadKey();
        }

        private static void Send()
        {
            var request = new Request
            {
                Command = "Classification",
                MissionId = 1,
                SensorId = "Sensor1"
            };
            _socket.Connect();
            var json = JsonConvert.SerializeObject(request, Formatting.Indented);
            _socket.SendAsync(json, b =>
            {
                Console.WriteLine(b ? $"{DateTime.Now} - Message Sent:\n{json}" : "Error Sending Message");
            });
        }

        private static void SendStop()
        {
            var request = new Request
            {
                Command = "Stop",
                MissionId = 1,
                SensorId = "Sensor1"
            };
            _socket.Connect();
            var json = JsonConvert.SerializeObject(request, Formatting.Indented);
            _socket.SendAsync(json, b =>
            {
                Console.WriteLine(b ? $"{DateTime.Now} - Message Sent:\n{json}" : "Error Sending Message");
            });
        }

        private static void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now} - Message Received:\n{e.Data}");
        }
    }
}
