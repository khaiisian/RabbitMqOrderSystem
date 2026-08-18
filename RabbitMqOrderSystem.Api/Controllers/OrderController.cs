using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMqOrderSystem.Api.Models;
using System.Text;
using System.Text.Json;

namespace RabbitMqOrderSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private const string QueueName = "orders";

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(Order order)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };

        // Opens the real network link to RabbitMQ.
        await using var connection = await factory.CreateConnectionAsync();

        // Opens the channel where all work happens. 
        await using var channel = await connection.CreateChannelAsync();

        // Guarantees the orders queue exists before we publish.
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // because RabbitMQ only ships bytes.
        var json = JsonSerializer.Serialize(order);
        var body = Encoding.UTF8.GetBytes(json);

        // actual send — drops the message into the orders queue
        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: QueueName,
            body: body);

        return Ok($"Order received: {order.Quantity} x {order.Product}");
    }
}
