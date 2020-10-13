using MessagePack.ImmutableCollection;
using Newtonsoft.Json.Linq;
using SekaiClient.Datas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace SekaiClient
{
    public class SekaiClient
    {
        public static Action<string> DebugWrite = text =>
        {
            var stack = new StackTrace();
            var method = stack.GetFrame(1).GetMethod();
            Console.WriteLine($"[{method.DeclaringType.Name}::{method.Name}]".PadRight(32) + text);
        };

        private const string urlroot = "http://production-game-api.sekai.colorfulpalette.org/api";
        private readonly HttpClient client;
        private readonly EnvironmentInfo environment;
        private string adid, uid, token;

        public SekaiClient(EnvironmentInfo info)
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            foreach (var field in typeof(EnvironmentInfo).GetFields())
            {
                if (field.FieldType != typeof(string)) continue;
                client.DefaultRequestHeaders.TryAddWithoutValidation(
                    field.Name.Replace('_', '-'),
                    field.GetValue(info) as string);
            }
            //client.DefaultRequestHeaders.TryAddWithoutValidation("Connection", "Keep-Alive");

            environment = info;
        }

        public JToken CallApi(string apiurl, HttpMethod method, JObject content)
        {
            var tick = DateTime.Now.Ticks;

            if (token != null)
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Session-Token", token);
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Request-Id", Guid.NewGuid().ToString());

            var body = new ByteArrayContent(PackHelper.Pack(content));
            body.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var resp = client.SendAsync(new HttpRequestMessage(method, urlroot + apiurl)
            {
                Content = body
            }).Result;

            var nextToken = resp.Headers.Contains("X-Session-Token") ? resp.Headers.GetValues("X-Session-Token").Single() : null;
            if (nextToken != null) token = nextToken;
            var result = PackHelper.Unpack(resp.Content.ReadAsByteArrayAsync().Result);

            client.DefaultRequestHeaders.Remove("X-Session-Token");
            client.DefaultRequestHeaders.Remove("X-Request-Id");
            DebugWrite(apiurl + $" called, {(DateTime.Now.Ticks - tick) / 1000 / 10.0} ms elapsed");
            return result;
        }

        public JToken CallUserApi(string apiurl, HttpMethod method, JObject content)
            => CallApi($"/user/{uid}" + apiurl, method, content);

        public void InitializeAdid()
        {

            using var client = new HttpClient();
            var json = new JObject
            {
                ["initiated_by"] = "sdk",
                ["apilevel"] = "29",
                ["event_buffering_enabled"] = "0",
                ["app_version"] = environment.X_App_Version,
                ["app_token"] = "6afszmodmiv4",
                ["os_version"] = "10",
                ["device_type"] = "phone",
                ["gps_adid"] = "20a417f1-46cb-4b26-9749-9b709be8ba60",
                ["android_uuid"] = "55d9b15f-3dbf-4e9e-b270-67351555b6db",
                ["device_name"] = "SEA-AL10",
                ["environment"] = "production",
                ["needs_response_details"] = "1",
                ["attribution_deeplink"] = "1",
                ["package_name"] = "com.sega.pjsekai",
                ["os_name"] = "android",
                ["gps_adid_src"] = "service",
                ["tracking_enabled"] = "1",
            };

            //json = JObject.Parse(client.PostAsync("https://app.adjust.com/session", new StringContent(json.ToString())).Result.Content.ReadAsStringAsync().Result);

            adid = "20f48346fad7f921245a8db7fdfb734f";
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-AI", adid);
        }

        public void Login(User user)
        {
            uid = user.uid;
            var json = CallUserApi($"/auth?refreshUpdatedResources=False", HttpMethod.Put, new JObject
            {
                ["credential"] = user.credit
            });
            token = json["sessionToken"].ToString();
            DebugWrite($"authenticated as {user.uid}");
        }

        public User Register()
        {
            var json = CallApi("/user", HttpMethod.Post, environment.CreateRegister());
            var uid = json["userRegistration"]["userId"].ToString();
            var credit = json["credential"].ToString();
            DebugWrite($"registered user {uid}");

            return new User
            {
                uid = uid,
                credit = credit
            };
        }

        public void PassTutorial(bool simplified = false)
        {
            //bypass turtorials
            CallUserApi($"/tutorial", HttpMethod.Patch, new JObject { ["tutorialStatus"] = "opening_1" });
            CallUserApi($"/tutorial", HttpMethod.Patch, new JObject { ["tutorialStatus"] = "gameplay" });
            CallUserApi($"/tutorial", HttpMethod.Patch, new JObject { ["tutorialStatus"] = "opening_2" });
            CallUserApi($"/tutorial", HttpMethod.Patch, new JObject { ["tutorialStatus"] = "unit_select" });
            CallUserApi($"/tutorial", HttpMethod.Patch, new JObject { ["tutorialStatus"] = "idol_opening" });
            CallUserApi($"/tutorial", HttpMethod.Patch, new JObject { ["tutorialStatus"] = "summary" });
            var presents = CallUserApi($"/home/refresh", HttpMethod.Put, new JObject { ["refreshableTypes"] = new JArray("login_bonus") })["updatedResources"]["userPresents"]
                .Select(t => t.Value<string>("presentId")).ToArray(); ;
            CallUserApi($"/tutorial", HttpMethod.Patch, new JObject { ["tutorialStatus"] = "end" });
            if (simplified) return;
            var episodes = new int[] { 50000, 50001, 40000, 40001, 30000, 30001, 20000, 20001, 60000, 60001, 4, 8, 12, 16, 20 };
            foreach (var episode in episodes)
                CallUserApi($"/story/unit_story/episode/{episode}", HttpMethod.Post, null);
            CallUserApi($"/present", HttpMethod.Post, new JObject { ["presentIds"] = new JArray(presents) });
            CallUserApi($"/costume-3d-shop/20006", HttpMethod.Post, null);
            CallUserApi($"/shop/2/item/10012", HttpMethod.Post, null);
            var currency = CallUserApi($"/mission/beginner_mission", HttpMethod.Put, new JObject
            {
                ["missionIds"] = new JArray(1, 2, 3, 4, 5, 6, 8, 10)
            })["updatedResources"]["user"]["userGamedata"]["chargedCurrency"]["free"];

            DebugWrite($"present received, now currency = {currency}");
        }

        public string[] Gacha()
        {
            IEnumerable<Card> icards = new Card[0];
            icards = icards.Concat(CallUserApi("/gacha/4/gachaBehaviorId/8", HttpMethod.Put, null)["obtainPrizes"]
                .Select(t => MasterData.cards[t["card"].Value<int>("resourceId").ToString()]));
            for (int i = 0; i < 6; ++i)
            icards = icards.Concat(CallUserApi("/gacha/4/gachaBehaviorId/7", HttpMethod.Put, null)["obtainPrizes"]
                .Select(t => MasterData.cards[t["card"].Value<int>("resourceId").ToString()]));
            icards = icards.Concat(CallUserApi("/gacha/2/gachaBehaviorId/4", HttpMethod.Put, null)["obtainPrizes"]
                .Select(t => MasterData.cards[t["card"].Value<int>("resourceId").ToString()]));

            var cards = icards.ToArray();
            var desc = cards
                .Select(card =>
                {
                    var character = MasterData.characters[card.characterId.ToString()];
                    var skill = MasterData.skills[card.skillId.ToString()];
                    return $"[{card.prefix}]".PadRightEx(30) + $"[{card.attr}]".PadRightEx(12) +
                        $"({character.gender.First()}){character.firstName}{character.givenName}".PadRightEx(20) +
                        skill.descriptionSpriteName.PadRightEx(20) + new string(Enumerable.Range(0, card.rarity).Select(_ => '*').ToArray());
                }).ToArray();

            DebugWrite($"gacha result:\n" + string.Join('\n', desc));
            int[] rares = new int[5];
            foreach (var card in cards) ++rares[card.rarity];
            if (rares[4] > 1)
                Console.WriteLine($"gacha result: {rares[4]}, {rares[3]}, {rares[2]}");
            return cards.Sum(card => card.rarity == 4 ? 1 : 0) > 2 ? desc : null;
        }

        public string Inherit(string password)
        {
            return CallUserApi("/inherit", HttpMethod.Put, new JObject { ["password"] = password })["userInherit"].Value<string>("inheritId");
        }

        public Account Serialize(string[] cards, string password = "1176321897") => new Account
        {
            inheritId = Inherit(password),
            password = password,
            cards = cards
        };

        public int APLive(int musicId, int boostCount, int deckId, string musicDifficulty = "expert", int score=100000000)
        {
            var music = MasterData.musics[musicId.ToString()];
            var md = music.musicDifficulties.Single(md => md.musicDifficulty == musicDifficulty);
            var liveid = CallUserApi("/live", HttpMethod.Post, new JObject
            {
                ["musicId"] = musicId,
                ["musicDifficultyId"] = md.id,
                ["musicVocalId"] = music.musicVocals.First().id,
                ["deckId"] = deckId,
                ["boostCount"] = boostCount
            })["userLiveId"];
            var result = CallUserApi("/live/" + liveid, HttpMethod.Put, new JObject
            {
                ["score"] = score,
                ["perfectCount"] = md.noteCount,
                ["greatCount"] = 0,
                ["goodCount"] = 0,
                ["badCount"] = 0,
                ["missCount"] = 0,
                ["maxCombo"] = md.noteCount,
                ["life"] = 1000,
                ["tapCount"] = md.noteCount,
                ["continueCount"] = 0
            });

            DebugWrite($"ap live done, now pt = {result["afterEventPoint"]}, currency = {result["updatedResources"]["user"]["userGamedata"]["chargedCurrency"]["free"]}" );
            return (int)result["afterEventPoint"];
        }

        public int[] GetCards()
        {
            var data = CallApi($"/suite/user/{uid}", HttpMethod.Get, null);

            return data["userCards"].Select(t => t.Value<int>("cardId")).ToArray();
        }

        public void ChangeDeck(int deckId, int[] cardIds)
        {
            CallUserApi("/deck/" + deckId, HttpMethod.Put, new JObject
            {
                ["userId"] = uid,
                ["deckId"] = deckId,
                ["name"] = "deck" + deckId,
                ["leader"] = cardIds[0],
                ["member1"] = cardIds[0],
                ["member2"] = cardIds[1],
                ["member3"] = cardIds[2],
                ["member4"] = cardIds[3],
                ["member5"] = cardIds[4],
            });
        }
    }
}
