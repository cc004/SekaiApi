using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SekaiClient;
using SekaiClient.Datas;

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
                            Console.WriteLine("writing to file.");
                            File.WriteAllText("accounts.json", JsonConvert.SerializeObject(accounts, Formatting.Indented));
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

            /*
            var client = new SekaiClient.SekaiClient(new EnvironmentInfo());
            client.InitializeAdid();
            var user = client.Register();
            File.WriteAllText("user.json", JsonConvert.SerializeObject(user));
            client.Login(user);
            client.PassTutorial();
            client.Gacha();
            var clist = new HashSet<int> { 1, 2, 3, 4, 24 };
            var chlist = new HashSet<int>();

            var cards = client.GetCards().OrderByDescending(id =>
            {
                var card = MasterData.cards[id.ToString()];
                var flag1 = clist.Contains(card.characterId);
                var flag2 = card.attr == "mysterious";
                return (flag1 ? (flag2 ? 0.5 : 0.2) : (flag2 ? 0.2 : 0)) + 0.0001 * card.rarity;
            }).Where(id =>
            {
                var card = MasterData.cards[id.ToString()];
                if (chlist.Contains(card.characterId))
                    return false;
                else
                {
                    chlist.Add(card.characterId);
                    return true;
                }
            }).ToArray();

            client.ChangeDeck(1, cards);
            Console.WriteLine(client.Inherit("1176321897"));
            while (true)
            {
                client.APLive(82, 0, 1);
            }
            */
            
            SekaiClient.SekaiClient.DebugWrite = _ => { };
            Enumerable.Range(0, 50).AsParallel().ForAll(_ => { while (true) Task(); });
        }
    }
}
