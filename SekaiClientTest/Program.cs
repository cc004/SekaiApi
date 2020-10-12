using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SekaiClient;

namespace SekaiClientTest
{
    class Program
    {
        private static List<Account> accounts;

        private static void Task()
        {
            try
            {
                var client = new SekaiClient.SekaiClient(new EnvironmentInfo());
                client.InitializeAdid();

                var user = client.Register();
                client.Login(user);
                client.PassTutorial();
                var result = client.Gacha();

                if (result != null)
                {
                    lock (accounts)
                    {
                        var account = client.Serialize(result.Where(c => c.EndsWith("***")).ToArray());
                        var star4s = result.Where(c => c.EndsWith("****")).ToArray();
                        account.nums = star4s.Length;
                        Console.WriteLine(string.Join("\n", star4s));
                        Console.WriteLine(string.Empty);
                        accounts.Add(account);
                        if (accounts.Count % 10 == 0)
                        {
                            Console.WriteLine("writing to file.");
                            File.WriteAllText("accounts.json", JsonConvert.SerializeObject(accounts, Formatting.Indented));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void Main(string[] args)
        {
            try
            {
                accounts = JsonConvert.DeserializeObject<List<Account>>(File.ReadAllText("accounts.json"));
            }
            catch
            {
                accounts = new List<Account>();
            }

            SekaiClient.SekaiClient.DebugWrite = _ => { };
            Enumerable.Range(0, 50).AsParallel().ForAll(_ => { while (true) Task(); });
        }
    }
}
