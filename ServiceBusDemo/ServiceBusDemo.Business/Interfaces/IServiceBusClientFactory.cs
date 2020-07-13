using Microsoft.Azure.ServiceBus.Core;
using ServiceBusDemo.Business.Models;
using System;

namespace ServiceBusDemo.Business.Interfaces
{
    public interface IServiceBusClientFactory
    {
        IReceiverClient GetDataReceiver(ClientType type, string endpoint, string path, string subscription = null, bool isSessionEnabled = false, Action<TextMessage> handler = null);
        ISenderClient GetDataSender(ClientType type, string endpoint, string path);
    }
}
