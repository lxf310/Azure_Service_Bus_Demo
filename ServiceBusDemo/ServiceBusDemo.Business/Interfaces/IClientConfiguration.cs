namespace ServiceBusDemo.Business.Interfaces
{
    public interface IClientConfiguration
    {
        bool IsOnPrem { get; }
        string AadClientId { get; }
        string TenantName { get; }
        string CertificateSubject { get; }
    }
}
