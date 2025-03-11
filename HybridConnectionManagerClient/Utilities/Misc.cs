using HcManProto;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace HybridConnectionManager.Library
{
    public static class Util
    {
        public static string EndpointRegexPattern = "^([a-zA-Z0-9.-]+):(\\d{1,5})$";
        public static string HcConnectionStringRegexPattern = @"^Endpoint=sb:\/\/[a-zA-Z0-9-]+\.servicebus\.windows\.net\/;SharedAccessKeyName=[a-zA-Z0-9-]+;SharedAccessKey=[a-zA-Z0-9+\/=]+;EntityPath=[a-zA-Z0-9-]+$";
        public static string RootConnectionStringRegexPattern = @"^Endpoint=sb:\/\/[a-zA-Z0-9-]+\.servicebus\.windows\.net\/;SharedAccessKeyName=[a-zA-Z0-9-]+;SharedAccessKey=[a-zA-Z0-9+\/=]+$";

        public static JsonSerializerSettings JsonNullHandlingSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        public static string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create),
            "HybridConnectionManagerV2");

        public static string AppDataLogDir = Path.Combine(AppDataPath, "Logs");

        public static string AppDataFilePath = Path.Combine(AppDataPath, "connections.json");

        public static string AppDataLogFileTemplate = Path.Combine(AppDataLogDir, "log_.txt");

        private static object _fileLock = new object();

        private const int BUFFER_LEN = 16384;

        /// <summary>
        /// Splits input string on whitespace characters.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string[] Tokenize(string input)
        {
            return Regex.Split(input, @"\s+");
        }

        public static async Task PipeStream(Stream fromStream, Stream toStream)
        {
            try
            {
                byte[] buffer = new byte[BUFFER_LEN];
                while (true)
                {
                    int bytesRead = await fromStream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        // Graceful close.
                        return;
                    }

                    await toStream.WriteAsync(buffer, 0, bytesRead);
                }
            }
            catch (Exception e)
            {
                // TODO: Log, and catch different kinds of exceptions.
                Console.WriteLine(e.ToString());
            }
        }

        [DataContract]
        private class KeyValuePair
        {
            [DataMember]
            public string key { get; set; }

            [DataMember]
            public string value { get; set; }
        }

        public static string GetEndpointStringFromUserMetadata(string metadata)
        {
            try
            {
                var stream = new MemoryStream(Encoding.Unicode.GetBytes(metadata.ToLower()));

                var serializer = new DataContractJsonSerializer(typeof(List<KeyValuePair>),
                    new DataContractJsonSerializerSettings() { });
                List<KeyValuePair> keyValuePairs = (List<KeyValuePair>)serializer.ReadObject(stream);

                foreach (var pair in keyValuePairs)
                {
                    if (string.Equals(pair.key, "endpoint", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return pair.value;
                    }
                }
            }
            catch (Exception e)
            {
                // TODO: log
                Console.WriteLine(e.ToString());
            }

            return null;
        }

        public static Tuple<string, int> GetEndpointFromUserMetadata(string metadata)
        {
            try
            {
                string endpoint = GetEndpointStringFromUserMetadata(metadata);

                if (endpoint == null)
                {
                    return null;
                }

                string[] parts = endpoint.Split(':');
                if (parts.Length != 2)
                {
                    return null;
                }

                var result = new Tuple<string, int>(parts[0], int.Parse(parts[1]));
                return result;
            }
            catch (Exception e)
            {
                // TODO: log?
                Console.WriteLine(e.ToString());
            }

            return null;
        }

        public static HybridConnectionInformation GetInformationFromConnectionString(string connectionString)
        {
            // connection string format:
            // Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<keyName>;SharedAccessKey=<keyValue>;EntityPath=<name>
            var splitProps = connectionString.Split(';');

            string endpointPair = splitProps[0];
            string endpoint = endpointPair[(endpointPair.IndexOf('=') + 1)..];
            string nameSpace = endpoint[5..endpoint.IndexOf('.')];

            string keyNamePair = splitProps[1];
            string keyName = keyNamePair[(keyNamePair.IndexOf('=') + 1)..];

            string keyValuePair = splitProps[2];
            string keyValue = keyValuePair[(keyValuePair.IndexOf('=') + 1)..];

            string namePair = splitProps[3];
            string name = namePair[(namePair.IndexOf('=') + 1)..];
            endpoint += name;

            return new HybridConnectionInformation()
            {
                Namespace = nameSpace,
                Name = name,
                KeyName = keyName,
                KeyValue = keyValue,
                Uri = endpoint,
            };
        }

        public static async Task<StringResponse> ConnectToEndpoint(string endpoint)
        {
            string host = endpoint.Split(':')[0];
            int port = int.Parse(endpoint.Split(':')[1]);

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(host, port);
                    return new StringResponse
                    {
                        Content = String.Format("Connection to {0}:{1} successful", host, port),
                        Error = false
                    };
                }
            }
            catch (Exception ex)
            {
                return new StringResponse
                {
                    Content = String.Format(ex.Message),
                    Error = true
                };
            }
        }

        public static List<HybridConnectionInformation> LoadConnectionsFromFilesystem()
        {
            List<HybridConnectionInformation> connections = new List<HybridConnectionInformation>();
            try
            {
                string jsonText = string.Empty;
                lock (_fileLock)
                {
                    jsonText = File.ReadAllText(AppDataFilePath);
                }

                if (!String.IsNullOrEmpty(jsonText))
                {
                    connections = JsonConvert.DeserializeObject<List<HybridConnectionInformation>>(jsonText, JsonNullHandlingSettings);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not deserialize connections from file: " + e.Message);
            }

            return connections ?? new List<HybridConnectionInformation>();
        }

        public static void CreateAppDataFile()
        {
            try
            {
                lock (_fileLock)
                {
                    Directory.CreateDirectory(AppDataPath);
                    File.WriteAllText(AppDataFilePath, "[]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not create connections file: " + ex.Message);
            }
        }

        public static void UpdateAppDataFile(List<HybridConnectionInformation> connectionInfos)
        {
            try
            {
                string jsonText = string.Empty;
                if (connectionInfos == null || connectionInfos.Count == 0)
                {
                    jsonText = "[]";
                }
                else
                {
                    jsonText = JsonConvert.SerializeObject(connectionInfos, Formatting.Indented, JsonNullHandlingSettings);
                }

                lock (_fileLock)
                {
                    File.WriteAllText(Util.AppDataFilePath, jsonText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not update connections file: " + ex.Message);
            }

        }

        public static void SetupFileDependencies()
        {
            if (!Directory.Exists(Util.AppDataPath))
            {
                Directory.CreateDirectory(Util.AppDataPath);
            }

            if (!Directory.Exists(Util.AppDataLogDir))
            {
                Directory.CreateDirectory(Util.AppDataLogDir);
            }

            if (!File.Exists(Util.AppDataFilePath))
            {
                Util.CreateAppDataFile();
            }
        }

        public static string GetIconFile()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Icon.ico";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "Icon.icns";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "Icon.png";
            }

            return "Icon.png";
        }

        public static bool TryParseHybridConnectionId(string armId, out string subscriptionId, out string resourceGroup, out string @namespace, out string name)
        {
            subscriptionId = null;
            resourceGroup = null;
            @namespace = null;
            name = null;

            var uriElements = armId.Split('/');

            try
            {
                subscriptionId = uriElements[2];
                resourceGroup = uriElements[4];
                @namespace = uriElements[8];
                name = uriElements[10];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        public static List<string> GetLogFiles()
        {
            if (!Directory.Exists(AppDataLogDir)) { return new List<string>(); }
            return Directory.GetFiles(AppDataLogDir).ToList();
        }

        public static string GetLogContent(string fileName)
        {
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return reader.ReadToEndAsync().Result;
        }
    }
}
