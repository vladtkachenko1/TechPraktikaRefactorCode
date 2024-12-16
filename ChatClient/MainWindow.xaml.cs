using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RedesignedChatClient
{
    public partial class MainWindow : Window, RedesignedChatLibrary.IChatClient
    {
        private RedesignedChatLibrary.IChatServer _server;
        private DuplexChannelFactory<RedesignedChatLibrary.IChatServer> _factory;
        private string _username;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Метод для підключення до сервера
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_server != null)
            {
                DisconnectFromServer();
                return;
            }

            _username = UsernameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(_username))
            {
                MessageBox.Show("Ім'я не може бути порожнім.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _factory = new DuplexChannelFactory<RedesignedChatLibrary.IChatServer>(this, "ChatServiceEndpoint");
                _server = _factory.CreateChannel();

                var registered = await Task.Run(() => _server.RegisterUser(_username));

                if (registered)
                {
                    ConnectButton.Content = "Відключитися";
                    UsernameTextBox.IsEnabled = false;
                    ConnectionStatus.Text = "Онлайн";
                    ConnectionStatus.Foreground = Brushes.Green;
                    Console.WriteLine($"Підключено як {_username}");
                }
                else
                {
                    MessageBox.Show("Це ім'я вже використовується.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка підключення", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Помилка: {ex.Message}");
            }
        }

        // Метод для відключення від сервера
        private void DisconnectFromServer()
        {
            try
            {
                _server?.RemoveUser(_username);
                (_server as IClientChannel)?.Close();
                _factory?.Close();
                ConnectionStatus.Text = "Офлайн";
                ConnectionStatus.Foreground = Brushes.Red;
                Console.WriteLine($"Відключено від сервера.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при відключенні: {ex.Message}");
            }
            finally
            {
                _server = null;
                _factory = null;
                ConnectButton.Content = "Підключитися";
                UsernameTextBox.IsEnabled = true;
            }
        }

        // Метод для відправки повідомлення
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTab = ChatTabs.SelectedItem as TabItem;

                if (selectedTab?.Header.ToString() == "Груповий чат")
                {
                    string message = MessageTextBox_Group.Text.Trim();
                    if (!string.IsNullOrEmpty(message))
                    {
                        _server.SendMessage(message, _username);
                        MessageTextBox_Group.Clear();
                    }
                }
                else if (selectedTab?.Header.ToString() == "Приватні повідомлення")
                {
                    string message = MessageTextBox_Private.Text.Trim();
                    string recipient = RecipientTextBox.Text.Trim();

                    if (!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(recipient))
                    {
                        _server.SendDirectMessage(message, _username, recipient);

                        AddPrivateMessageToUI(_username, recipient, message, isSender: true);

                        MessageTextBox_Private.Clear();
                    }
                    else
                    {
                        MessageBox.Show("Введіть повідомлення та отримувача.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при відправці повідомлення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Помилка при відправці повідомлення: {ex.Message}");
            }
        }

        // Метод для обробки натискання клавіші Enter в полі вводу
        private void MessageTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SendButton_Click(sender, e);
            }
        }

        // Обробка вибору користувача зі списку
        private void UserList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserList.SelectedItem != null)
                RecipientTextBox.Text = UserList.SelectedItem.ToString();
        }

        // Метод для отримання повідомлення
        public void ReceiveMessage(string message, string sender)
        {
            Dispatcher.Invoke(() =>
            {
                ChatDisplay.AppendText($"{sender}: {message}\n");
                Console.WriteLine($"Отримано повідомлення від {sender}: {message}");
            });
        }

        // Метод для отримання приватного повідомлення
        public void ReceiveDirectMessage(string message, string sender)
        {
            Dispatcher.Invoke(() =>
            {
                AddPrivateMessageToUI(sender, _username, message, isSender: false);
                Console.WriteLine($"Отримано приватне повідомлення від {sender}: {message}");
            });
        }

        // Оновлення списку онлайн користувачів
        public void UpdateUserList(List<string> onlineUsers)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    UserList.Items.Clear();
                    foreach (var user in onlineUsers)
                    {
                        UserList.Items.Add(user);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при оновленні списку користувачів: {ex.Message}");
            }
        }

        private void AddPrivateMessageToUI(string sender, string recipient, string message, bool isSender)
        {
            var messagePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };

            var senderText = new TextBlock
            {
                Text = isSender ? $"{sender} (to {recipient}): " : $"{sender} (Приватне): ",
                Foreground = Brushes.Green,
                FontWeight = FontWeights.Bold
            };

            var messageText = new TextBlock
            {
                Text = message,
                Foreground = Brushes.DarkGreen,
                TextWrapping = TextWrapping.Wrap
            };

            messagePanel.Children.Add(senderText);
            messagePanel.Children.Add(messageText);

            PrivateMessagesList.Items.Add(messagePanel);
        }
    }
}
