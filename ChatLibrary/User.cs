using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace RedesignedChatLibrary
{
    [ServiceContract(CallbackContract = typeof(IChatClient))]
    public interface IChatServer
    {
        [OperationContract]
        bool RegisterUser(string username);

        [OperationContract]
        void RemoveUser(string username);

        [OperationContract]
        void SendMessage(string message, string sender);

        [OperationContract]
        void SendDirectMessage(string message, string sender, string recipient);
    }

    public interface IChatClient
    {
        [OperationContract]
        void ReceiveMessage(string message, string sender);

        [OperationContract]
        void UpdateUserList(List<string> onlineUsers);

        [OperationContract]
        void ReceiveDirectMessage(string message, string sender); 
    }
    [ServiceContract]
    public interface IChatService
    {
        [OperationContract]
        void BroadcastMessage(string sender, string message);

        [OperationContract]
        void SendPrivateMessage(string recipient, string sender, string message);

        [OperationContract]
        void Connect(string username);
    }
    public class User
    {
        public string Username { get; set; }
        public OperationContext Context { get; set; }
        public IChatClient Callback { get; set; }
    }
}
