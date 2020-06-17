using System;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;

namespace EasyNetQServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus("host=localhost;username=guest;password=guest");
            var advancedBus = bus.Advanced;
            
            var queue = await advancedBus.QueueDeclareAsync("rpc_server");

            var consume = advancedBus.Consume(queue, async (bytes, properties, info) =>
            {
                var replyTo = properties.ReplyTo;
                var correlationId = properties.CorrelationId;
                
                Console.WriteLine($"Received rpc call, replyTo: {replyTo}, correlationId: {correlationId}");

                var request = Encoding.UTF8.GetString(bytes);
                
                await advancedBus.PublishAsync(Exchange.GetDefault(), replyTo, false, new MessageProperties
                {
                    CorrelationId = correlationId
                }, Encoding.UTF8.GetBytes(request));
            });

            Console.WriteLine("waiting for requests...");
            Console.ReadLine();
        }
    }
}