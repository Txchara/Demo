using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

class Program
{
    static async Task Main(string[] args)
    {
        // 本地 MQTT Broker 地址。
        string broker = "127.0.0.1";

        // 本地 Mosquitto 默认明文端口。
        int port = 1883;

        // 发送目标主题，和接收端保持一致。
        string topic = "test/1";

        // 为当前发送端生成唯一客户端标识，避免与其他客户端冲突。
        string clientId = $"mqtt-send-{Guid.NewGuid():N}";

        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port)
            .WithClientId(clientId)
            .WithCleanSession()
            .Build();

        try
        {
            var connectResult = await mqttClient.ConnectAsync(options);

            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                Console.WriteLine($"连接 MQTT Broker 失败，结果代码：{connectResult.ResultCode}");
                return;
            }

            Console.WriteLine("已成功连接到本地 MQTT Broker。");
            Console.WriteLine($"当前发送主题：{topic}");
            Console.WriteLine("请输入要发送的消息，输入 exit 后退出程序。");

            while (true)
            {
                Console.Write("请输入消息内容：");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("消息内容不能为空，请重新输入。");
                    continue;
                }

                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("收到退出指令，准备关闭发送端。");
                    break;
                }

                // 构造要发送的 MQTT 消息。
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(Encoding.UTF8.GetBytes(input))
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                // 发布到 Broker，Broker 再把消息分发给订阅了该主题的客户端。
                await mqttClient.PublishAsync(message);
                Console.WriteLine($"消息已发送，主题：{topic}，内容：{input}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT 运行时发生异常：{ex.Message}");
        }
        finally
        {
            if (mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
                Console.WriteLine("已断开与 MQTT Broker 的连接。");
            }
        }
    }
}
