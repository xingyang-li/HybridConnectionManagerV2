using HybridConnectionManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HybridConnectionManager.Service
{
    public static class Util
    {
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

            Console.WriteLine(endpoint);

            return new HybridConnectionInformation()
            {
                Namespace = nameSpace,
                Name = name,
                KeyName = keyName,
                KeyValue = keyValue,
                Uri = endpoint,
            };
        }
    }
}
