﻿using Newtonsoft.Json;
using System;
using System.Reflection;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using StartreckSimulator.Models;
using WebSocketSharp;

namespace TestClient
{
    class Program
    {
        private static WebSocket socket;
        static void Main(string[] args)
        {
            Console.WriteLine("Please Enter WebSocket Url:");
            var url = Console.ReadLine();
            try
            {
                socket = new WebSocket(url);
            }
            catch
            {

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invaild Url! try again...");
                Console.ResetColor();
                Main(args);
            }
            socket.OnMessage += Socket_OnMessage;

            Console.WriteLine("Press Any Key to Send Request. Or Esc to Exit");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
                var request = new Request
                {
                    MessageType = MessageType.Request,
                    Command = "Classification",
                    MissionId = 1,
                    SensorId = "Sensor1",
                    RequestId = "12345"
                };

                var root = new
                {
                    Request = request
                };
                Send(request);
            }

            var stop = new Request
            {
                MessageType = MessageType.Request,
                Command = "Stop",
                MissionId = 1,
                SensorId = "Sensor1",
                RequestId = "12345"
            };

            var stopRoot = new
            {
                Stop = stop
            };
            Send(stop);
            Console.ReadKey();
        }

        private static void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now} - Message Received:\n{e.Data}");

            Acknowledge ack = new Acknowledge
            {
                MessageType = MessageType.Acknowledge,
                Code = 200,
                Message = "",
                RequestId = "12345"
            };

            var root = new
            {
                Acknowledge = ack
            };
            Send(ack);
        }

        private static void Send(object data)
        {
            try
            {
                if (!socket.IsAlive)
                {
                    socket.Connect();
                }

                var json = data.ToJson();
                socket.Send(json);
                Console.WriteLine($"{DateTime.Now} - Message Sent:\n{json}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }
        }
    }
}
