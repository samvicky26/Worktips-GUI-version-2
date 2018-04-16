using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace WorktipsWallet
{
    class ConnectionManager
    {
        public static int rpcID = 0;
        public static string _rpcRand = new Random().Next(10000000, 999999999).ToString();

        private static JObject _request(string method, Dictionary<string,object> args)
        {
            var builtURL = Properties.Settings.Default.RPCprotocol + "://" + Properties.Settings.Default.RPCdestination + ":" + Properties.Settings.Default.RPCport + Properties.Settings.Default.RPCtrailing;
            var payload = new Dictionary<string, object>()
            {
                { "jsonrpc", "2.0" },
                { "password", _rpcRand},
                { "method", method },
                { "params", args },
                { "id", rpcID.ToString() }
            };

            string payloadJSON = JsonConvert.SerializeObject(payload, Formatting.Indented);
            rpcID++;

            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            var cli = new WorktipsClient();
            cli.Headers[HttpRequestHeader.ContentType] = "application/json";
            string response = cli.UploadString(builtURL, payloadJSON);

            var jobj = JObject.Parse(response);
            if (jobj.ContainsKey("error"))
            {
                throw new Exception("Walletd RPC failed with error: " + Convert.ToInt32(jobj["error"]["code"]).ToString() + "  " + jobj["error"]["message"]);
            }

            return (JObject)jobj["result"];
        }

        public static string _requestRPC(string method, string args)
        {
            var args_dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(args);
            var builtURL = Properties.Settings.Default.RPCprotocol + "://" + Properties.Settings.Default.RPCdestination + ":" + Properties.Settings.Default.RPCport + Properties.Settings.Default.RPCtrailing;
            var payload = new Dictionary<string, object>()
            {
                { "jsonrpc", "2.0" },
                { "password", _rpcRand},
                { "method", method },
                { "params", args_dict },
                { "id", rpcID.ToString() }
            };

            string payloadJSON = JsonConvert.SerializeObject(payload, Formatting.Indented);
            rpcID++;

            var cli = new WebClient();
            cli.Headers[HttpRequestHeader.ContentType] = "application/json";
            string response = cli.UploadString(builtURL, payloadJSON);

            return response;
        }

        public static Tuple<bool,string,JObject> Request(string method, Dictionary<string, object> args = null)
        {
            if (args == null) args = new Dictionary<string, object>() { };

            try
            {
                var results = _request(method, args);
                return Tuple.Create<bool, string, JObject>(true, "", results);
            }
            catch (Exception e)
            {
                return Tuple.Create<bool,string,JObject>(false, e.Message, null);
            }
        }

        public static Tuple<bool,string, Process> StartDaemon(string wallet, string pass)
        {
            var curDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var walletdexe = System.IO.Path.Combine(curDir, "walletd.exe");

            if (IsRunningOnMono())
            {
                walletdexe = System.IO.Path.Combine(curDir, "walletd");
            }

            if (!System.IO.File.Exists(wallet))
            {
                return Tuple.Create<bool,string,Process>(false, "Wallet file cannot be found! Must exit!", null);
            }

            var conflictingProcesses = Process.GetProcessesByName("walletd")
                               .Concat(Process.GetProcessesByName("Worktipsd")).ToArray();

            int numConflictingProcesses = conflictingProcesses.Length;

            for (int i = 0; i < numConflictingProcesses; i++)
            {
                /* Need to kill all walletd and Worktipsd processes so
                   they don't lock the DB */
                conflictingProcesses[i].Kill();
            }

            /* Delete walletd.log if it exists so we can ensure when reading
               the file later upon a crash, that we are reporting the proper
               crash reason and not some previous crash */
            System.IO.File.Delete("walletd.log");

            Process p = new Process();
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.FileName = walletdexe;
            p.StartInfo.Arguments = CLIEncoder.Encode(new string[] {"-w", wallet, "-p", pass, "--local", "--rpc-password", _rpcRand});

            int maxConnectionAttempts = 5;

            /* It takes a small amount of time to kill the other processes
               if needed, so lets try and connect a few times before failing. */
            for (int i = 0; i < maxConnectionAttempts; i++)
            {
                p.Start();
                System.Threading.Thread.Sleep(1500);

                if (!p.HasExited)
                {
                    return Tuple.Create<bool, string, Process>(true, "", p);
                }
            }

            return Tuple.Create<bool, string, Process>(false, "Unable to keep daemon up!", null);
        }

        public static Tuple<bool,JObject> Get_live_stats()
        {
            string pool_eu = "http://199.247.21.36:8117/live_stats";
            string pool_us = "http://199.247.21.36:8117/live_stats";
            string content = "";

            try
            {
                var cli = new DecompressClient();
                cli.Headers[HttpRequestHeader.ContentType] = "application/json";
                content = cli.DownloadString(pool_eu);
            }
            catch (Exception) { }

            if (content == "")
            {
                try
                {
                    var cli = new DecompressClient();
                    cli.Headers[HttpRequestHeader.ContentType] = "application/json";
                    content = cli.DownloadString(pool_us);
                }
                catch (Exception) { }
            }

            if (content == "")
            {
                return Tuple.Create<bool, JObject>(false, null);
            }
            else
            {
                var jobj = JObject.Parse(content);
                return Tuple.Create<bool, JObject>(true, jobj);
            }
        }

        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }
    }

    public static class JsonConversionExtensions
    {
        public static IDictionary<string, object> ToDictionary(this JObject json)
        {
            var propertyValuePairs = json.ToObject<Dictionary<string, object>>();
            ProcessJObjectProperties(propertyValuePairs);
            ProcessJArrayProperties(propertyValuePairs);
            return propertyValuePairs;
        }

        private static void ProcessJObjectProperties(IDictionary<string, object> propertyValuePairs)
        {
            var objectPropertyNames = (from property in propertyValuePairs
                                       let propertyName = property.Key
                                       let value = property.Value
                                       where value is JObject
                                       select propertyName).ToList();

            objectPropertyNames.ForEach(propertyName => propertyValuePairs[propertyName] = ToDictionary((JObject)propertyValuePairs[propertyName]));
        }

        private static void ProcessJArrayProperties(IDictionary<string, object> propertyValuePairs)
        {
            var arrayPropertyNames = (from property in propertyValuePairs
                                      let propertyName = property.Key
                                      let value = property.Value
                                      where value is JArray
                                      select propertyName).ToList();

            arrayPropertyNames.ForEach(propertyName => propertyValuePairs[propertyName] = ToArray((JArray)propertyValuePairs[propertyName]));
        }

        public static object[] ToArray(this JArray array)
        {
            return array.ToObject<object[]>().Select(ProcessArrayEntry).ToArray();
        }

        private static object ProcessArrayEntry(object value)
        {
            if (value is JObject)
            {
                return ToDictionary((JObject)value);
            }
            if (value is JArray)
            {
                return ToArray((JArray)value);
            }
            return value;
        }
    }

    class DecompressClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return request;
        }
    }
    class WorktipsClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.Timeout = 60000;
            return request;
        }
    }
}
