using System;
using System.ServiceModel;
using RedesignedChatLibrary;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            using (var host = new ServiceHost(typeof(RedesignedChatLibrary.ChatServer)))
            {
                try
                {
                    host.Open(); 
                    Console.WriteLine("Server is running at net.tcp://localhost:9000/");
                    Console.WriteLine("Press Enter to stop the server...");
                    Console.ReadLine();
                }
                catch (CommunicationException ce)
                {
                    Console.WriteLine($"Communication error: {ce.Message}");
                }
                catch (TimeoutException te)
                {
                    Console.WriteLine($"Timeout error: {te.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error starting server: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }
}
