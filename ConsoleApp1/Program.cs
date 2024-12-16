using System;
using System.ServiceModel;
using RedesignedChatLibrary;

class TestClient
{
    static void Main(string[] args)
    {
        var binding = new NetTcpBinding();
        var address = new EndpointAddress("net.tcp://localhost:9000/ChatService");

        var factory = new ChannelFactory<IChatServer>(binding, address);

        try
        {
            var server = factory.CreateChannel();
            if (server.RegisterUser("TestUser"))
            {
                Console.WriteLine("Connected to the server.");
            }
            else
            {
                Console.WriteLine("Failed to register user.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to the server: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
