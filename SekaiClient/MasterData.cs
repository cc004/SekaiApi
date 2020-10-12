using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SekaiClient.Datas
{
    [JsonObject]
    public class Card
    {
        public int characterId, rarity, skillId;
        public string prefix, attr;
    }

    [JsonObject]
    public class Character
    {
        public string firstName, givenName, gender;
    }

    [JsonObject]
    public class Skill
    {
        public string descriptionSpriteName;
    }

    public static class MasterData
    {
        public static Dictionary<string, Character> characters;
        public static Dictionary<string, Card> cards;
        public static Dictionary<string, Skill> skills;

        static MasterData()
        {
            var master = JObject.Parse(File.ReadAllText("master_data.json"));
            characters = master["gameCharacters"].ToObject<Dictionary<string, Character>>();
            cards = master["cards"].ToObject<Dictionary<string, Card>>();
            skills = master["skills"].ToObject<Dictionary<string, Skill>>();
        }
    }
}
