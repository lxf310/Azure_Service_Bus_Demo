using ServiceBusDemo.Business.Interfaces;
using System;
using System.Configuration;

namespace ServiceBusDemo.Business
{
    public class ClientConfiguration : ConfigurationSection, IClientConfiguration
    {
        #region Properties

        private const string ClientConfigurationSection = "clientConfiguration";
        private const string IsOnPremProp = "isOnPrem";
        private const string AadClientIdProp = "aadClientId";
        private const string TenantNameProp = "tenantName";
        private const string CertificateSubjectProp = "certificateSubject";
        public static readonly Lazy<IClientConfiguration> Default;

        #endregion


        #region Methods

        static ClientConfiguration()
        {
            Default = new Lazy<IClientConfiguration>(() => getInstance());
        }


        private static IClientConfiguration getInstance()
        {
            return (IClientConfiguration)ConfigurationManager.GetSection(ClientConfigurationSection);
        }

        #endregion


        #region IClientConfiguration

        [ConfigurationProperty(IsOnPremProp)]
        public bool IsOnPrem
        {
            get { return (bool)this[IsOnPremProp]; }
            set { this[IsOnPremProp] = value; }
        }


        [ConfigurationProperty(AadClientIdProp)]
        public string AadClientId
        {
            get { return (string)this[AadClientIdProp]; }
            set { this[AadClientIdProp] = value; }
        }


        [ConfigurationProperty(TenantNameProp)]
        public string TenantName
        {
            get { return (string)this[TenantNameProp]; }
            set { this[TenantNameProp] = value; }
        }


        [ConfigurationProperty(CertificateSubjectProp)]
        public string CertificateSubject
        {
            get { return (string)this[CertificateSubjectProp]; }
            set { this[CertificateSubjectProp] = value; }
        }

        #endregion
    }
}
