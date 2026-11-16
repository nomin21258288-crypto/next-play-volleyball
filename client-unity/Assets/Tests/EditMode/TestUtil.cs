// Shared helpers. Works under Unity Test Runner AND plain `dotnet test` (tools/SimTests).
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NextPlay.Sim;
using NUnit.Framework;

namespace NextPlay.Tests
{
    public static class TestUtil
    {
        public const double Tol = 1e-9;

        public static JObject Golden()
        {
            string rel = "Assets/Tests/EditMode/Golden/golden.json";
            string[] starts = { TestContext.CurrentContext.TestDirectory, Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
            foreach (string s in starts)
                for (DirectoryInfo d = new DirectoryInfo(s); d != null; d = d.Parent)
                {
                    string a = Path.Combine(d.FullName, rel); if (File.Exists(a)) return JObject.Parse(File.ReadAllText(a));
                    string b = Path.Combine(d.FullName, "client-unity", rel); if (File.Exists(b)) return JObject.Parse(File.ReadAllText(b));
                }
            throw new FileNotFoundException("golden.json not found (run: node tools/gen_golden.mjs)");
        }

        public static V3 ToV3(JToken t) { return new V3((double)t["x"], (double)t["y"], (double)t["z"]); }

        public static void Run(Game g, double seconds, Func<Game, Dictionary<string, PlayerInput>> inputs = null, BotFn bot = null)
        {
            if (bot == null) bot = Bot.Input;
            long n = (long)Math.Round(seconds / Const.DT);
            for (long i = 0; i < n && g.Phase != Phase.Over; i++) g.Tick(inputs != null ? inputs(g) : null, bot);
        }

        public static int Count(Game g, string action)
        {
            int n = 0; foreach (GameEvent e in g.Events) if (e.Type == "action" && e.Ok == true && e.Action == action) n++; return n;
        }
    }
}
