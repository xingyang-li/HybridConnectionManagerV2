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

        private static string GetNIndents(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException("n", n, "n must be positive.");
            return n > 0
                ? string.Concat(Enumerable.Range(0, n).Select(_ => "\t"))
                : string.Empty;
        }

        public override string ToString()
        {
            return ToString(indentDepth: 0);
        }

        public string ToString(int indentDepth = 0, bool showKeys = false)
        {
            string indents = GetNIndents(indentDepth);
            StringBuilder strBuilder = new();
            strBuilder
                .AppendLine($"{indents}{{")
                .AppendLine($"{indents}\tUri: {Uri},");
            if (!string.IsNullOrEmpty(Namespace) && !string.IsNullOrEmpty(Name))
            {
                strBuilder
                    .AppendLine($"{indents}\tNamespace: {Namespace},")
                    .AppendLine($"{indents}\tName: {Name},");
            }
            if (!string.IsNullOrEmpty(EndpointHost))
            {
                strBuilder.AppendLine($"{indents}\tEndpoint: {EndpointHost}:{EndpointPort},");
            }
            if (showKeys)
            {
                strBuilder
                    .AppendLine($"{indents}\tKeyName: {KeyName},")
                    .AppendLine($"{indents}\tKeyValue: {KeyValue},");
            }
            strBuilder.AppendLine($"{indents}}}");
            return strBuilder.ToString();
        }
    }
}
