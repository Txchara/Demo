using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Newtonsoft.Json;

class Program
{
    static async Task Main(string[] args)
    {
        // 本地 MQTT Broker 地址。
        // 如果 Broker 与当前程序运行在同一台电脑上，使用 127.0.0.1 最直接。
        string broker = "127.0.0.1";

        // MQTT 默认明文端口通常是 1883。
        // 如果你使用的是带 TLS 的安全连接，端口一般会变成 8883。
        int port = 1883;

        // 每个 MQTT 客户端都应该有一个唯一的 ClientId。
        // 这里使用 Guid 动态生成，避免与其他客户端重复。
        string clientId = Guid.NewGuid().ToString();

        // 当前程序要订阅的主题。
        // 只有发往这个主题的消息，当前程序才会收到。
        string topic = "DTUQoS: 0";

        // MqttFactory 用来创建 MQTT 客户端实例。
        var factory = new MqttFactory();

        // 创建一个真正负责连接、订阅、接收消息的客户端对象。
        var mqttClient = factory.CreateMqttClient();


        // 注册“收到消息时”的回调函数。
        // 当 Broker 把订阅主题上的消息推送给当前客户端时，这段代码会被触发。
        mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        static Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var Topic = e.ApplicationMessage.Topic;
            var Payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            try
            {
                var root = JsonDocument.Parse(Payload).RootElement;

                ParseDeviceData(root);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ JSON解析失败: " + ex.Message);
                Console.WriteLine("原始数据: " + Payload);
            }

            return Task.CompletedTask;
        }

        

        // 构造 MQTT 连接参数。
        // 这里使用的是本地 Broker + 明文 TCP 连接，不启用 TLS。
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port)
            .WithClientId(clientId)

            // CleanSession 表示本次连接使用干净会话。
            // 程序断开后，Broker 不会为这个客户端保留上一轮的订阅状态和离线消息。
            .WithCleanSession()
            .Build();

        try
        {
            // 发起与 MQTT Broker 的连接。
            var connectResult = await mqttClient.ConnectAsync(options);

            // 如果连接结果不是成功，直接输出失败原因并结束程序。
            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                Console.WriteLine($"连接 MQTT Broker 失败，结果代码：{connectResult.ResultCode}");
                return;
            }

            // 连接成功后，订阅指定主题。
            // 从这一刻开始，只要 Broker 收到发往 test/1 的消息，就会转发给当前程序。
            await mqttClient.SubscribeAsync(topic);

            Console.WriteLine("已成功连接到本地 MQTT Broker。");
            Console.WriteLine($"已订阅主题：{topic}");
            Console.WriteLine("现在可以使用 MQTTX 向该主题发送消息。");
            Console.WriteLine("按回车键退出程序。");

            // 阻塞主线程，保持程序运行。
            // 如果这里不等待，程序会立刻结束，来不及接收 MQTTX 发送的消息。
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            // 捕获连接、订阅、网络异常等错误，方便定位问题。
            Console.WriteLine($"MQTT 运行时发生异常：{ex.Message}");
        }
        finally
        {
            // 如果当前仍然处于连接状态，程序退出前主动断开。
            // 这是一个比较稳妥的资源释放动作。
            if (mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
                Console.WriteLine("已断开与 MQTT Broker 的连接。");
            }
        }
    }

    static void ParseDeviceData(JsonElement root)
    {
        foreach (var item in root.EnumerateObject())
        {
            string key = item.Name;

            // ✅ 参数
            if (key.StartsWith("参数_"))
            {
                int value = item.Value.GetInt32();
                Console.WriteLine($"参数 -> {key} = {value}");
            }

            // 🚨 报警（重点）
            else if (key.StartsWith("报警_"))
            {
                int value = item.Value.GetInt32();

                Console.WriteLine($"报警 -> {key} = {value}");
            }

            // 📊 统计
            else if (key == "当天产量" || key == "总产量")
            {
                int value = item.Value.GetInt32();
                Console.WriteLine($"统计 -> {key} = {value}");
            }
        }
    }
}
