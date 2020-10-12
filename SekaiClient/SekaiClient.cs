using MessagePack.ImmutableCollection;
using Newtonsoft.Json.Linq;
using SekaiClient.Datas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private readonly Dictionary<HttpMethod, Func<string, HttpContent, Task<HttpResponseMessage>>> methodDict;
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
            methodDict = new Dictionary<HttpMethod, Func<string, HttpContent, Task<HttpResponseMessage>>>();
            methodDict[HttpMethod.Get] = (s, c) => client.GetAsync(s);
            methodDict[HttpMethod.Post] = client.PostAsync;
            methodDict[HttpMethod.Put] = client.PutAsync;
            methodDict[HttpMethod.Patch] = client.PatchAsync;
        }

        public JToken CallApi(string apiurl, HttpMethod method, JObject content)
        {
            var tick = DateTime.Now.Ticks;
            var request = WebRequest.CreateHttp(urlroot + apiurl);
            request.Method = method.Method;
            request.Headers.Clear();
            if (token != null)
                request.Headers.Add("X-Session-Token", token);
            foreach (var field in typeof(EnvironmentInfo).GetFields())
            {
                if (field.FieldType != typeof(string)) continue;
                request.Headers.Add(
                    field.Name.Replace('_', '-'),
                    field.GetValue(environment) as string);
            }
            request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());
            if (adid != null)
                request.Headers.Add("X-AI", adid);

            if (content != null)
            {
                var body = PackHelper.Pack(content);
                request.GetRequestStream().Write(body, 0, body.Length);
            }

            var resp = request.GetResponse();

            var nextToken = resp.Headers.Get("X-Session-Token");
            if (nextToken != null) token = nextToken;
            var result = PackHelper.Unpack(resp.GetResponseStream().ReadToEnd());

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

        public void PassTutorial()
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

            var episodes = new int[] { 50000, 50001, 40000, 40001, 30000, 30001, 20000, 20001, 60000, 60001, 4, 8, 12, 16, 20 };
            foreach (var episode in episodes)
                CallUserApi($"/story/unit_story/episode/{episode}", HttpMethod.Post, null);
            CallUserApi($"/present", HttpMethod.Post, new JObject { ["presentIds"] = new JArray(presents) });
            CallUserApi($"/costume-3d-shop/20006", HttpMethod.Post, null);
            CallUserApi($"/shop/2/item/10046", HttpMethod.Post, null);
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
            if (rares[4] > 0)
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
    }
}
