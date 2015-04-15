using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using NetFwTypeLib;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Collections;

namespace TorBlocker
{
    class Program
    {
        #region Commands
        const string BLOCK_CMD = "block";
        const string REMOVE_CMD = "remove";
        const string RELOAD_CMD = "reload";
        #endregion

        static int Main(string[] args)
        {
            try
            {
                string ruleName = ConfigurationManager.AppSettings["ruleName"];

                if (args.Count() != 1)
                    Console.WriteLine("Invalid args count");

                switch (args[0].ToLower())
                {
                    case BLOCK_CMD:
                        BlockIpList(GetTorIpList(), ruleName);
                        break;
                    case REMOVE_CMD:
                        RemoveAllRulesByName(ruleName);
                        break;
                    case RELOAD_CMD:
                        RemoveAllRulesByName(ruleName);
                        BlockIpList(GetTorIpList(), ruleName);
                        break;
                    default:
                        Console.WriteLine("Invalid command");
                        break;
                }

                return 0;
            }
            catch
            {
                return -1;
            }
        }

        static void BlockIpList(IEnumerable<string> ipList, string name)
        {
            if (ipList == null || !ipList.Any())
                return;

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            foreach (string ip in ipList)
            {
                try
                {
                    INetFwRule2 firewallRule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));

                    firewallRule.Name = name;
                    firewallRule.RemoteAddresses = ip;
                    firewallRule.InterfaceTypes = "All";
                    firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                    firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                    firewallRule.Enabled = true;

                    firewallPolicy.Rules.Add(firewallRule);
                }
                catch (UnauthorizedAccessException e)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
            }
        }

        static void RemoveAllRulesByName(string name)
        {
            int rulesCount = GetRulesByName(name).Count();

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            for (int i = 0; i<rulesCount; ++i)
            {
                    firewallPolicy.Rules.Remove(name);
            }
        }

        static IEnumerable<INetFwRule> GetRulesByName(string name)
        {
            List<INetFwRule> rules = new List<INetFwRule>();

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            foreach (INetFwRule2 rule in firewallPolicy.Rules)
            {
                if (rule.Name == name)
                {
                    rules.Add(rule);
                }
            }
            return rules;
        }

        static IEnumerable<string> GetTorIpList()
        {
            string currentHost = ConfigurationManager.AppSettings["currentHost"];
            string torBulkUrlPattern = ConfigurationManager.AppSettings["torBulkUrl"];

            string torBulkUrl = String.Format(torBulkUrlPattern, currentHost);

            IEnumerable<string> addresses = null;

            using (WebClient client = new WebClient())
            {
                string response = client.DownloadString(torBulkUrl);

                // dumb but first 3 are comments
                addresses = response.Split('\n').Skip(3).Take(response.Length - 3);
            }

            return addresses;
        }
    }
}
