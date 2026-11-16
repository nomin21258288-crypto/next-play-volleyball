// Replays the JS reference matches in C# and compares every event and the final state.
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NextPlay.Sim;
using NUnit.Framework;

namespace NextPlay.Tests
{
    public class GoldenParityTests
    {
        static readonly PlayerAction[] Act = { PlayerAction.Serve, PlayerAction.Pass, PlayerAction.Set, PlayerAction.Spike, PlayerAction.Block };

        // mirrors `scripted(i)` in tools/gen_golden.mjs (integer patterns only, no trig)
        static PlayerInput Scripted(int i)
        {
            PlayerInput p = PlayerInput.Move((i % 200) < 100 ? 0.8 : -0.8, (i % 90) < 45 ? 0.5 : -0.5);
            if (i % 300 == 0) p.Jump = true;
            if (i % 40 == 0) { p.Action = Act[(i / 40) % 5]; p.AimZ = (i % 3 - 1) * 0.5; }
            return p;
        }

        static void Compare(JToken trace, Game g, string label)
        {
            JArray exp = (JArray)trace["events"];
            Assert.AreEqual(exp.Count, g.Events.Count, label + ": event count");
            for (int i = 0; i < exp.Count; i++)
            {
                JToken e = exp[i]; GameEvent a = g.Events[i]; string at = label + " event#" + i + " (" + (string)e["type"] + ")";
                Assert.AreEqual((string)e["type"], a.Type, at);
                Assert.That(a.T, Is.EqualTo((double)e["t"]).Within(TestUtil.Tol), at + " t");
                if (e["player"] != null) Assert.AreEqual((string)e["player"], a.Player, at + " player");
                if (e["team"] != null) Assert.AreEqual((int)e["team"], a.Team, at + " team");
                if (e["action"] != null) Assert.AreEqual((string)e["action"], a.Action, at + " action");
                if (e["ok"] != null) Assert.AreEqual((bool)e["ok"], a.Ok, at + " ok");
                if (e["reason"] != null) Assert.AreEqual((string)e["reason"], a.Reason, at + " reason");
                if (e["score"] != null) CollectionAssert.AreEqual(e["score"].ToObject<int[]>(), a.Score, at + " score");
                if (e["sets"] != null) CollectionAssert.AreEqual(e["sets"].ToObject<int[]>(), a.Sets, at + " sets");
            }
            JToken f = trace["final"];
            Assert.That(g.T, Is.EqualTo((double)f["t"]).Within(TestUtil.Tol), label + " final t");
            Assert.That(g.Ball.Pos.X, Is.EqualTo((double)f["ball"]["x"]).Within(TestUtil.Tol), label + " ball x");
            Assert.That(g.Ball.Pos.Y, Is.EqualTo((double)f["ball"]["y"]).Within(TestUtil.Tol), label + " ball y");
            Assert.That(g.Ball.Pos.Z, Is.EqualTo((double)f["ball"]["z"]).Within(TestUtil.Tol), label + " ball z");
            CollectionAssert.AreEqual(f["score"].ToObject<int[]>(), g.Match.Score, label + " score");
            CollectionAssert.AreEqual(f["sets"].ToObject<int[]>(), g.Match.Sets, label + " sets");
            Assert.AreEqual((int)f["server"], g.Match.Server, label + " server");
            JArray pl = (JArray)f["players"];
            for (int i = 0; i < pl.Count; i++)
            {
                Assert.AreEqual((string)pl[i]["id"], g.Players[i].Id);
                Assert.That(g.Players[i].X, Is.EqualTo((double)pl[i]["x"]).Within(TestUtil.Tol), label + " player x " + i);
                Assert.That(g.Players[i].Z, Is.EqualTo((double)pl[i]["z"]).Within(TestUtil.Tol), label + " player z " + i);
                Assert.That(g.Players[i].Y, Is.EqualTo((double)pl[i]["y"]).Within(TestUtil.Tol), label + " player y " + i);
            }
        }

        [Test] public void Golden_AllBotMatches_MatchJsReference()
        {
            foreach (JToken t in (JArray)TestUtil.Golden()["traces"])
            {
                if (t["scriptedHuman"] != null) continue;
                Game g = new Game(new string[0], 2, new Coach { Seed = (uint)t["seed"] });
                int ticks = (int)t["ticks"];
                for (int i = 0; i < ticks && g.Phase != Phase.Over; i++) g.Tick(null, Bot.Input);
                Compare(t, g, "bots seed " + (int)t["seed"]);
            }
        }

        [Test] public void Golden_ScriptedHumanInput_MatchesJsReference()
        {
            foreach (JToken t in (JArray)TestUtil.Golden()["traces"])
            {
                if (t["scriptedHuman"] == null) continue;
                Game g = new Game(new[] { "H0" }, 2, new Coach { Seed = (uint)t["seed"] });
                int ticks = (int)t["ticks"];
                for (int i = 0; i < ticks && g.Phase != Phase.Over; i++)
                    g.Tick(new Dictionary<string, PlayerInput> { { "H0", Scripted(i) } }, Bot.Input);
                Compare(t, g, "scripted human");
            }
        }
    }
}
