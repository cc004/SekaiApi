using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SekaiClient
{
    [JsonObject]
    public class Account
    {
        public User account;
        public int nums;
        public string[] cards;
    }
}
