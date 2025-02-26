namespace HybridConnectionManagerGUI.Models
{
    public class HybridConnectionModel
    {
        public string Namespace
        {
            get; set;
        }

        public string Name
        {
            get; set;
        }

        public string ConnectionString
        {
            get; set;
        }

        public string Endpoint
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
    }

    public class HybridConnectionsModel
    {
        public List<HybridConnectionModel> Connections { get; set; }
    }
}
