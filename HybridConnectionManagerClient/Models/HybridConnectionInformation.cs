using System.Text;

namespace HybridConnectionManager.Models
{
    public class HybridConnectionInformation
    {
        public string Namespace
        {
            get; set;
        }

        public string Name
        {
            get; set;
        }

        public string Uri
        {
            get; set;
        }

        public string KeyName
        {
            get; set;
        }
        public string KeyValue
        {
            get; set;
        }

        public string EndpointHost
        {
            get; set;
        }

        public int EndpointPort
        {
            get; set;
        }

        public string ResourceGroup
        {
            get; set;
        }

        public string SubscriptionId
        {
            get; set;
        }

        public string Status
        {
            get; set;
        }

        public string CreatedOn
        {
            get; set;
        }

        public string LastUpdated
        {
            get; set;
        }

        public int NumberOfListeners
        {
            get; set;
        }
    }
}
