using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace RedesignedChatLibrary
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class ChatServer : IChatServer
    {
        private readonly ConcurrentDictionary<string, IChatClient> _clients = new ConcurrentDictionary<string, IChatClient>();

        // Реєстрація користувача
        public bool RegisterUser(string username)
        {
            try
            {
                var callback = OperationContext.Current.GetCallbackChannel<IChatClient>();
                if (callback == null)
                {
                    Console.WriteLine("Callback channel is null!");
                    return false;
                }

                if (!_clients.TryAdd(username, callback))
                {
                    return false;
                }

                Console.WriteLine($"User {username} connected.");
                BroadcastUserListAsync();
                BroadcastMessageAsync("System", $"{username} has joined the chat.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RegisterUser: {ex.Message}");
                return false;
            }
        }

        // Оновлення списку онлайн користувачів
        private async Task BroadcastUserListAsync()
        {
            var onlineUsers = _clients.Keys.ToList();
            var tasks = _clients.Values.Select(client =>
                Task.Run(() =>
                {
                    try
                    {
                        client.UpdateUserList(onlineUsers);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating user list: {ex.Message}");
                    }
                })).ToList();

            await Task.WhenAll(tasks);
        }

        // Видалення користувача
        public void RemoveUser(string username)
        {
            if (_clients.TryRemove(username, out _))
            {
                Console.WriteLine($"User {username} disconnected.");
                BroadcastUserListAsync();
                BroadcastMessageAsync("System", $"{username} has left the chat.");
            }
        }

        // Відправка повідомлення в груповий чат
        public void SendMessage(string message, string sender)
        {
            BroadcastMessageAsync(sender, message);
        }

        // Відправка приватного повідомлення
        public void SendDirectMessage(string message, string sender, string recipient)
        {
            if (_clients.TryGetValue(recipient, out var recipientClient))
            {
                try
                {
                    recipientClient.ReceiveDirectMessage(message, sender);
                    Console.WriteLine($"Приватне повідомлення від {sender} до {recipient}: {message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка відправки повідомлення до {recipient}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Отримувач {recipient} не знайдений.");
            }
        }

        // Відправка повідомлення всім користувачам
        private async Task BroadcastMessageAsync(string sender, string message)
        {
            var disconnectedUsers = new ConcurrentBag<string>();
            var exceptions = new ConcurrentBag<Exception>();

            var tasks = _clients.Select(user =>
                Task.Run(() =>
                {
                    try
                    {
                        user.Value.ReceiveMessage(message, sender);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(new Exception($"Error sending message to {user.Key}: {ex.Message}"));
                        disconnectedUsers.Add(user.Key);
                    }
                })).ToList();

            await Task.WhenAll(tasks);

            foreach (var user in disconnectedUsers)
            {
                _clients.TryRemove(user, out _);
                Console.WriteLine($"User {user} has been disconnected due to an error.");
            }

            if (!disconnectedUsers.IsEmpty)
            {
                await BroadcastUserListAsync();
            }

            if (!exceptions.IsEmpty)
            {
                Console.WriteLine("Errors occurred while broadcasting messages:");
                foreach (var ex in exceptions)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
