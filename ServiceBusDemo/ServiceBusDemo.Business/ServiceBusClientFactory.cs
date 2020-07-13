using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using ServiceBusDemo.Business.Interfaces;
using ServiceBusDemo.Business.Models;
using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ServiceBusDemo.Business
{
    public class ServiceBusClientFactory : IServiceBusClientFactory
    {
        #region Properties

        private readonly IClientConfiguration _config;
        private const string AAD = "https://login.microsoftonline.com/{0}";
        private const string ServiceBusAudience = "https://servicebus.azure.net";

        #endregion


        #region Constructor

        public ServiceBusClientFactory(IClientConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException($"{nameof(config)} could not be null.");
        }

        #endregion


        #region Methods

        public IReceiverClient GetDataReceiver(ClientType type, string endpoint, string path, string subscription = null, bool isSessionEnabled = false, Action<TextMessage> handler = null)
        {
            if (type == ClientType.Topic && isSessionEnabled)
            {
                throw new ArgumentException("Service bus topic does not support session.");
            }

            if (type == ClientType.Topic && string.IsNullOrEmpty(subscription))
            {
                throw new ArgumentException("To get a client of service bus topic, a subscription name must be provided.");
            }

            var policy = new RetryExponential(TimeSpan.Zero, TimeSpan.FromSeconds(30), 5);
            IReceiverClient client;

            switch (type)
            {
                case ClientType.Queue:
                    client = new QueueClient(endpoint, path, createTokenProvider(), TransportType.Amqp, ReceiveMode.PeekLock, policy);
                    break;
                case ClientType.Topic:
                    client = new SubscriptionClient(endpoint, path, subscription, createTokenProvider(), TransportType.Amqp, ReceiveMode.PeekLock, policy);
                    break;
                default:
                    throw new ArgumentException("Unknown type");
            }

            if (handler != null)
            {
                if (isSessionEnabled)
                {
                    var options = new SessionHandlerOptions(exceptionReceivedHandlerAsync);
                    options.AutoComplete = false;
                    options.MaxConcurrentSessions = 10;
                    options.MaxAutoRenewDuration = new TimeSpan(0, 5, 0);
                    ((IQueueClient)client).RegisterSessionHandler(async (messageSession, message, cancellationToken) =>
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        try
                        {
                            handler?.Invoke(JsonConvert.DeserializeObject<TextMessage>(Encoding.ASCII.GetString(message.Body)));
                            await completeAsync(message, client);
                        }
                        catch (Exception)
                        {
                            await abandonAsync(message, client);
                        }
                        finally
                        {
                            await messageSession.CloseAsync();
                        }
                    }, options);
                }
                else
                {
                    var options = new MessageHandlerOptions(exceptionReceivedHandlerAsync);
                    options.AutoComplete = false;
                    options.MaxConcurrentCalls = 10;
                    options.MaxAutoRenewDuration = new TimeSpan(0, 5, 0);
                    client.RegisterMessageHandler(async (message, cancellationToken) =>
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        try
                        {
                            handler?.Invoke(JsonConvert.DeserializeObject<TextMessage>(Encoding.ASCII.GetString(message.Body)));
                            await completeAsync(message, client);
                        }
                        catch (Exception)
                        {
                            await abandonAsync(message, client);
                        }
                    }, options);
                }
            }

            return client;
        }


        public ISenderClient GetDataSender(ClientType type, string endpoint, string path)
        {
            var policy = new RetryExponential(TimeSpan.Zero, TimeSpan.FromSeconds(30), 5);
            ISenderClient client;
            switch (type)
            {
                case ClientType.Queue:
                    client = new QueueClient(endpoint, path, createTokenProvider(), TransportType.Amqp, ReceiveMode.PeekLock, policy);
                    break;
                case ClientType.Topic:
                    client = new TopicClient(endpoint, path, createTokenProvider(), TransportType.Amqp, policy);
                    break;
                default:
                    throw new ArgumentException("Unknown type");
            }
            return client;
        }


        private X509Certificate2 getCertificateBySubject(string subject)
        {
            if (string.IsNullOrEmpty(subject)) return null;

            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, subject, false);
                if (certificates.Count != 1)
                {
                    throw new Exception($"Failed to retrieve the certificate with thumbprint {subject}.");
                }
                return certificates[0];
            }
        }


        private TokenProvider createTokenProvider()
        {
            if (_config.IsOnPrem)
            {
                X509Certificate2 cert = getCertificateBySubject(_config.CertificateSubject);
                var auth = string.Format(CultureInfo.InvariantCulture, AAD, _config.TenantName);
                var tokenProvider = TokenProvider.CreateAzureActiveDirectoryTokenProvider(async (audience, authority, state) =>
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(_config.AadClientId)
                                    .WithAuthority(authority)
                                    .WithCertificate(cert)
                                    .Build();

                    var serviceBusAudience = new Uri(ServiceBusAudience);

                    var authResult = await app.AcquireTokenForClient(new string[] { $"{serviceBusAudience}/.default" }).ExecuteAsync();
                    return authResult.AccessToken;

                }, auth);

                return tokenProvider;
            }
            else
            {
                return TokenProvider.CreateManagedIdentityTokenProvider();
            }
        }


        private Task exceptionReceivedHandlerAsync(ExceptionReceivedEventArgs arg)
        {
            return Task.CompletedTask;
        }


        protected async Task completeAsync(Message message, IReceiverClient client)
        {
            await client.CompleteAsync(message.SystemProperties.LockToken);
        }

        protected async Task abandonAsync(Message message, IReceiverClient client)
        {
            await client.AbandonAsync(message.SystemProperties.LockToken);
        }

        #endregion
    }
}
