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
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SekaiClient;
using SekaiClient.Datas;

namespace SekaiClientTest
{
    class Program
    {
        private static List<Account> accounts;

        private static async Task Task()
        {
            try
            {
                var tick = DateTime.Now.Ticks;
                var client = new SekaiClient.SekaiClient(new EnvironmentInfo());
                client.InitializeAdid();
                await client.UpgradeEnvironment();

                var user = await client.Register();
                await client.Login(user);
                await client.PassTutorial();
                var result = await client.Gacha();

                if (result != null)
                {
                    var account = await client.Serialize(result.Where(c => c.EndsWith("***")).ToArray());
                    lock (accounts)
                    {
                        var star4s = result.Where(c => c.EndsWith("****")).ToArray();
                        account.nums = star4s.Length;
                        Console.WriteLine(string.Join("\n", star4s));
                        Console.WriteLine(string.Empty);
                        accounts.Add(account);
                            Console.WriteLine("writing to file.");
                            File.WriteAllText("accounts.json", JsonConvert.SerializeObject(accounts, Formatting.Indented));
                    }
                }
                //Console.WriteLine($"task done, {(DateTime.Now.Ticks - tick) / 1000 / 10.0}ms elapsed");
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
            //var user = client.Register();
            var user = JsonConvert.DeserializeObject<User>(File.ReadAllText("user.json"));
            File.WriteAllText("user.json", JsonConvert.SerializeObject(user));
            client.Login(user);
            //client.PassTutorial();
            //client.Gacha();
            var clist = new HashSet<int> { 1, 2, 3, 4, 24 };
            var chlist = new HashSet<int>();
            /*
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

            Console.WriteLine("bonus : " + cards.Take(5).Select((id =>
            {
                var card = MasterData.cards[id.ToString()];
                var flag1 = clist.Contains(card.characterId);
                var flag2 = card.attr == "mysterious";
                return (flag1 ? (flag2 ? 0.5 : 0.2) : (flag2 ? 0.2 : 0)) + 0.0001 * card.rarity;
            })).Sum() * 100 + "%");

            client.ChangeDeck(1, cards);
            Console.WriteLine(client.Inherit("1176321897"));

            while (true)
                client.APLive(47, 1, 1, "master");
            */
            object @lock = new object();
            Console.WriteLine(ThreadPool.SetMaxThreads(1000, 2000));
            //SekaiClient.SekaiClient.DebugWrite = _ => { };

            for (int i = 0; i < 64; ++i)
                ThreadPool.QueueUserWorkItem(async _ => { while (true) await Task(); });

            Thread.Sleep(int.MaxValue);
        }
    }
}
