using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace RedesignedChatLibrary
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class ChatServer : IChatServer
    {
        private readonly Dictionary<string, IChatClient> _clients = new Dictionary<string, IChatClient>();
        private readonly object _lock = new object();

        // Реєстрація користувача
        public bool RegisterUser(string username)
        {
            try
            {
                lock (_lock)
                {
                    if (_clients.ContainsKey(username))
                    {
                        return false; 
                    }

                    var callback = OperationContext.Current.GetCallbackChannel<IChatClient>();
                    if (callback == null)
                    {
                        Console.WriteLine("Callback channel is null!");
                        return false;
                    }

                    _clients[username] = callback;
                    Console.WriteLine($"User {username} connected.");

                    BroadcastUserListAsync();
                    BroadcastMessageAsync("System", $"{username} has joined the chat.");
                    return true;
                }
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
            var tasks = new List<Task>();

            foreach (var client in _clients.Values)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        client.UpdateUserList(onlineUsers);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating user list: {ex.Message}");
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        // Видалення користувача
        public void RemoveUser(string username)
        {
            lock (_lock)
            {
                if (_clients.Remove(username))
                {
                    Console.WriteLine($"User {username} disconnected.");
                    BroadcastUserListAsync();
                    BroadcastMessageAsync("System", $"{username} has left the chat.");
                }
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
            lock (_lock)
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
        }

        // Відправка повідомлення всім користувачам
        private async Task BroadcastMessageAsync(string sender, string message)
        {
            var disconnectedUsers = new List<string>();
            var tasks = new List<Task>();

            foreach (var user in _clients)
            {
                tasks.Add(Task.Run(() =>
                {
                try
                {
                    user.Value.ReceiveMessage(message, sender);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message to {user.Key}: {ex.Message}");
                        lock (_lock)
                        {
                            disconnectedUsers.Add(user.Key);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            lock (_lock)
            {
                foreach (var user in disconnectedUsers)
                {
                    _clients.Remove(user);
                    Console.WriteLine($"User {user} has been disconnected due to an error.");
                }
            }

            if (disconnectedUsers.Any())
            {
                await BroadcastUserListAsync();
            }
        }
    }
}

