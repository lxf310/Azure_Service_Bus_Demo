using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using ServiceBusDemo.Business;
using ServiceBusDemo.Business.Models;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ServiceBuseDemo.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientFactory = new ServiceBusClientFactory(ClientConfiguration.Default.Value);
            const string endpoint = "FakeCache.servicebus.windows.net";
            const string path = "test_queue";
            var receiver = clientFactory.GetDataReceiver(ClientType.Queue, endpoint, path, null, false, (textMessage) => {
                Console.WriteLine($"{textMessage.Id}: {textMessage.Text}");
            });
            var sender = clientFactory.GetDataSender(ClientType.Queue, endpoint, path);

            var tasks = new Task[10];
            Random rnd = new Random();
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = sender.SendAsync(new Message
                {
                    Body = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new TextMessage
                    {
                        Id = Guid.NewGuid(),
                        Text = $"This is text {i}."
                    }))
                });
                System.Threading.Thread.Sleep(rnd.Next(10) * 100);
            }
            Task.WaitAll(tasks);

            Console.ReadKey();
        }
    }
}
