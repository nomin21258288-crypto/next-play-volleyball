using System;
using System.Collections.Generic;
using NextPlay.Sim;
using NUnit.Framework;

namespace NextPlay.Tests
{
    public class SimGameplayTests
    {
        static Game AllBots(uint seed = 12345) { return new Game(new string[0], 2, new Coach { Seed = seed }); }

        [Test] public void AllBotMatchCompletes_EveryActionOccurs_RulesEndTheMatch()
        {
            Game g = AllBots(); TestUtil.Run(g, 1800);
            Assert.AreEqual(Phase.Over, g.Phase); Assert.AreNotEqual(-1, g.Match.Winner);
            foreach (string a in new[] { "serve", "pass", "set", "spike", "block" }) Assert.Greater(TestUtil.Count(g, a), 0, "missing " + a);
            Assert.IsTrue(g.Match.Sets[0] == 2 || g.Match.Sets[1] == 2);
        }
        [Test] public void SameSeedGivesIdenticalMatch()
        {
            Game a = AllBots(7), b = AllBots(7); TestUtil.Run(a, 300); TestUtil.Run(b, 300);
            Assert.AreEqual(a.Events.Count, b.Events.Count); Assert.AreEqual(a.Ball.Pos.X, b.Ball.Pos.X); Assert.AreEqual(a.T, b.T);
        }
        [Test] public void BallAndPlayersStayInSaneBounds()
        {
            Game g = AllBots();
            for (int i = 0; i < 60 * 120; i++)
            {
                g.Tick(null, Bot.Input);
                Assert.IsFalse(double.IsNaN(g.Ball.Pos.X) || double.IsNaN(g.Ball.Pos.Y) || double.IsNaN(g.Ball.Pos.Z));
                Assert.Less(Math.Abs(g.Ball.Pos.X), 60); Assert.Less(Math.Abs(g.Ball.Pos.Y), 60);
                foreach (Player p in g.Players) { Assert.LessOrEqual(Math.Abs(p.X), 11); Assert.LessOrEqual(Math.Abs(p.Z), 7); }
            }
        }
        [Test] public void JumpIsPhysical_ApexMatchesVSquaredOver2g()
        {
            Game g = new Game(new[] { "H0" }); double apex = 0;
            for (int i = 0; i < 120; i++)
            {
                Dictionary<string, PlayerInput> inp = new Dictionary<string, PlayerInput>();
                PlayerInput pi = default(PlayerInput); pi.Jump = i == 0; inp["H0"] = pi;
                g.Tick(inp, Bot.Input); apex = Math.Max(apex, g.Players[0].Y);
            }
            Assert.That(apex, Is.EqualTo(4.6 * 4.6 / (2 * 9.81)).Within(0.05));
        }
        [Test] public void HumanMovementIsSpeedLimitedAndKeptOnOwnSide()
        {
            Game g = new Game(new[] { "H0" }); Player h = g.Players[0]; double x0 = h.X;
            for (int i = 0; i < 120; i++) { var inp = new Dictionary<string, PlayerInput>(); inp["H0"] = PlayerInput.Move(1, 0); g.Tick(inp, Bot.Input); }
            Assert.That(h.X, Is.LessThanOrEqualTo(-0.3));                         // cannot cross the net
            Assert.That(h.X - x0, Is.LessThanOrEqualTo(Cfg.Speed * 1.0 + 1e-6));   // <= speed * 1s
        }
        [Test] public void OnlyTheServerCanServe_AndServeStartsTheRally()
        {
            Game g = new Game(new[] { "H0" });                                       // H0 is the opening server
            PlayerInput pass = default(PlayerInput); pass.Action = PlayerAction.Pass;
            g.Tick(new Dictionary<string, PlayerInput> { { "H0", pass } }, Bot.Input);
            Assert.AreEqual(Phase.Serve, g.Phase);                                   // a pass during serve phase does nothing
            for (int i = 0; i < 120 * 3 && g.Phase == Phase.Serve; i++)
            {
                Player h = g.Players[0]; PlayerInput inp = PlayerInput.Move(-9.4 - h.X, -h.Z);
                if (g.ServeWait > 1) inp.Action = PlayerAction.Serve;
                g.Tick(new Dictionary<string, PlayerInput> { { "H0", inp } }, Bot.Input);
            }
            Assert.AreEqual(Phase.Rally, g.Phase); Assert.AreEqual(1, TestUtil.Count(g, "serve"));
        }
        [Test] public void PassThatCannotHappenIsRejectedAndReportedToHuman()
        {
            Game g = new Game(new[] { "H0" }); TestUtil.Run(g, 1.2, null, (gg, p) => default(PlayerInput));   // idle everyone
            int before = g.Events.Count; PlayerInput pass = default(PlayerInput); pass.Action = PlayerAction.Pass;
            g.Tick(new Dictionary<string, PlayerInput> { { "H0", pass } }, (gg, p) => default(PlayerInput));
            TestUtil.Run(g, 0.5, null, (gg, p) => default(PlayerInput));
            GameEvent fail = g.Events.GetRange(before, g.Events.Count - before).Find(e => e.Player == "H0" && e.Action == "pass");
            Assert.IsNotNull(fail); Assert.AreEqual(false, fail.Ok);
        }
        [Test] public void BotsSpikeAndBlockInRealRallies()
        {
            Game g = AllBots(); TestUtil.Run(g, 600);
            Assert.Greater(TestUtil.Count(g, "block"), 0); Assert.Greater(TestUtil.Count(g, "spike"), 0);
        }
        [Test] public void NoUnityTypesLeakIntoSim()
        {
            foreach (Type t in typeof(Game).Assembly.GetTypes())
                foreach (var f in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static))
                    Assert.IsFalse(f.FieldType.Namespace != null && f.FieldType.Namespace.StartsWith("UnityEngine"), t.Name + "." + f.Name);
        }
    }
}
