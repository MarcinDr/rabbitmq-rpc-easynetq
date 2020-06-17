using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace EasyNetQClient
{
    class Program
    {
        private static readonly BlockingCollection<string> respQueue = new BlockingCollection<string>();
        private const string ReplyToQueue = "amq.rabbitmq.reply-to";
        // private const string ReplyToQueue = "test";
        
        static async Task Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus("host=localhost;username=guest;password=guest");
            var advancedBus = bus.Advanced;

            var queue = await advancedBus.QueueDeclareAsync(ReplyToQueue);
            var correlationId = Guid.NewGuid().ToString();

            var consume = advancedBus.Consume(queue, (bytes, properties, info) =>
            {
                Console.WriteLine($"Received message: {properties.ToJson()}, info : {info.ToJson()}");
                if (properties.CorrelationId == correlationId)
                {
                    var response = Encoding.UTF8.GetString(bytes);
                    respQueue.Add(response);
                }
            });


            await advancedBus.PublishAsync(Exchange.GetDefault(), "rpc_server", false, new MessageProperties
            {
                ReplyTo = ReplyToQueue,
                CorrelationId = correlationId
            }, Encoding.UTF8.GetBytes("30"));
            
            Console.WriteLine("Message sent, waiting for response...");
            
            var response = respQueue.Take();
            
            Console.WriteLine($"Response: {response}");
            Console.ReadLine();
        }
    }

    static class Extensions
    {
        public static string ToJson(this object value)
        {
            return JsonSerializer.Serialize(value);
        }
        
        // private static string ToJson(this byte[] value)
        // {
        //     return JsonSerializer.Serialize(value);
        // }
    }
}