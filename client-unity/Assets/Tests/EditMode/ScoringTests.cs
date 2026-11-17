// Custom-room scoring (7..25) and Ranked fixed rules. Mirrors packages/sim-core/test/scoring.test.js.
// Compiled by Unity EditMode AND plain `dotnet test`.
using System;
using System.Collections.Generic;
using NextPlay.Sim;
using NUnit.Framework;

namespace NextPlay.Tests
{
    public class ScoringTests
    {
        // One point for `team` through the real rules code (the other team serves out of turn and forfeits the point).
        static void Give(MatchState m, int team) { m.Server = team; Rules.OnServe(m, 1 - team, "T"); }
        static MatchState Custom(int target, int sets = 1) { return new MatchState { Rules = MatchRules.Custom(target, sets) }; }
        static LogEntry LastSetEnd(MatchState m) { LogEntry r = null; foreach (LogEntry e in m.Log) if (!e.IsPoint) r = e; return r; }
        public static IEnumerable<int> AllTargets() { for (int n = 7; n <= 25; n++) yield return n; }

        [Test] public void EveryWholeNumber7To25IsAccepted_AndNothingElseIs()
        {
            Assert.AreEqual(19, RoomConfig.ScoreOptions().Count);
            Assert.AreEqual(7, RoomConfig.ScoreOptions()[0]); Assert.AreEqual(25, RoomConfig.ScoreOptions()[18]);
            foreach (int n in AllTargets()) { Assert.IsTrue(MatchRules.IsValidTarget(n)); Assert.AreEqual(n, MatchRules.Custom(n).SetTargets[0]); }
            foreach (int bad in new[] { 6, 26, 0, -1, 100, int.MinValue, int.MaxValue })
                Assert.Throws<ArgumentOutOfRangeException>(() => MatchRules.Custom(bad), "accepted " + bad);
        }

        [TestCaseSource(nameof(AllTargets))] public void CustomTarget_EndsExactlyAtN_NothingEarlier(int n)
        {
            MatchState m = Custom(n);
            for (int i = 1; i < n; i++) { Give(m, 0); Assert.AreEqual(-1, m.Winner, "ended early at " + i); Assert.AreEqual(i, m.Score[0]); Assert.AreEqual(0, m.Score[1]); }
            Give(m, 0);
            Assert.AreEqual(0, m.Winner); Assert.AreEqual(1, m.Sets[0]); Assert.AreEqual(n, LastSetEnd(m).Score[0]);
        }

        [Test] public void WinByTwoAppliesToTheChosenTarget()
        {
            MatchState m = Custom(7);
            for (int i = 0; i < 6; i++) { Give(m, 0); Give(m, 1); }
            Give(m, 0); Assert.AreEqual(-1, m.Winner);
            Give(m, 1); Give(m, 0); Assert.AreEqual(-1, m.Winner);
            Give(m, 0); Assert.AreEqual(0, m.Winner); Assert.AreEqual(9, LastSetEnd(m).Score[0]); Assert.AreEqual(7, LastSetEnd(m).Score[1]);
        }

        [Test] public void EitherTeamCanWin() { MatchState m = Custom(11); for (int i = 0; i < 11; i++) Give(m, 1); Assert.AreEqual(1, m.Winner); Assert.AreEqual(1, m.Sets[1]); }

        [Test] public void CustomSetsToWin2_UsesChosenTargetInEverySet()
        {
            MatchState m = Custom(9, 2);
            for (int i = 0; i < 9; i++) Give(m, 0);
            Assert.AreEqual(-1, m.Winner); Assert.AreEqual(1, m.Sets[0]); Assert.AreEqual(1, m.SetIndex);
            for (int i = 0; i < 9; i++) Give(m, 1);
            Assert.AreEqual(1, m.Sets[1]); Assert.AreEqual(2, m.SetIndex);
            for (int i = 0; i < 9; i++) Give(m, 0);
            Assert.AreEqual(0, m.Winner);
        }

        [Test] public void Ranked_KeepsFixedOfficialRules_AndHasNoCustomTarget()
        {
            MatchRules r = MatchRules.Ranked();
            Assert.AreEqual("ranked", r.Mode); Assert.AreEqual(2, r.SetsToWin); Assert.AreEqual(2, r.WinBy); CollectionAssert.AreEqual(new[] { 25, 25, 15 }, r.SetTargets);
            CollectionAssert.AreEqual(new[] { 25, 25, 15 }, new MatchState().Rules.SetTargets);            // default state = ranked (older code unchanged)
            MatchState m = new MatchState();
            for (int i = 0; i < 24; i++) Give(m, 0);
            Assert.AreEqual(24, m.Score[0]); Give(m, 0); Assert.AreEqual(1, m.Sets[0]);
            for (int i = 0; i < 25; i++) Give(m, 1);
            Assert.AreEqual(1, m.Sets[1]);
            for (int i = 0; i < 14; i++) Give(m, 0);
            Assert.AreEqual(-1, m.Winner); Give(m, 0); Assert.AreEqual(0, m.Winner);                         // deciding set to 15
            Assert.Throws<ArgumentException>(() => RoomConfig.Create("u", "2v2", "ranked", 15));
            Assert.Throws<ArgumentException>(() => RoomConfig.Create("u", "2v2", "ranked", 7));
        }

        [Test] public void CreateRoom_AllThreeFormats_AcceptEveryScore()
        {
            foreach (string f in RoomConfig.Formats)
                foreach (int t in AllTargets()) { RoomConfig room = RoomConfig.Create("u1", f, "custom", t); Assert.AreEqual(t, room.Rules.SetTargets[0]); Assert.AreEqual(RoomConfig.PlayersFor(f), room.PlayersPerTeam); }
            CollectionAssert.AreEqual(new[] { "1v1", "2v2", "6v6" }, RoomConfig.Formats);
            Assert.Throws<ArgumentOutOfRangeException>(() => RoomConfig.Create("u1", "3v3", "custom", 10));
            Assert.Throws<ArgumentException>(() => RoomConfig.Create("", "1v1", "custom", 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => RoomConfig.Create("u1", "2v2", "custom", null));
            Assert.Throws<ArgumentOutOfRangeException>(() => RoomConfig.Create("u1", "2v2", "custom", 26));
        }

        [Test] public void OnlyTheCreatorOfACustomUnstartedRoomCanChangeTheScore()
        {
            RoomConfig room = RoomConfig.Create("host", "2v2", "custom", 15);
            Assert.AreEqual(7, room.WithTargetScore("host", 7).Rules.SetTargets[0]); Assert.AreEqual(25, room.WithTargetScore("host", 25).Rules.SetTargets[0]);
            Assert.AreEqual(15, room.Rules.SetTargets[0]);
            Assert.Throws<InvalidOperationException>(() => room.WithTargetScore("guest", 9));
            Assert.Throws<ArgumentOutOfRangeException>(() => room.WithTargetScore("host", 26)); Assert.Throws<ArgumentOutOfRangeException>(() => room.WithTargetScore("host", 6));
            room.Started = true; Assert.Throws<InvalidOperationException>(() => room.WithTargetScore("host", 9));
            RoomConfig ranked = RoomConfig.Create("host", "2v2", "ranked");
            Assert.Throws<InvalidOperationException>(() => ranked.WithTargetScore("host", 9));
        }

        [Test] public void ChosenScoreDrivesTheRealSimulation_AllFormats()
        {
            foreach (string f in RoomConfig.Formats)
                foreach (int t in new[] { 7, 12, 25 })
                {
                    Game g = RoomConfig.Create("h", f, "custom", t).StartGame(new string[0], new Coach { Seed = 5 });
                    Assert.AreEqual(RoomConfig.PlayersFor(f) * 2, g.Players.Count); Assert.AreEqual(t, g.Match.Rules.SetTargets[0]);
                    TestUtil.Run(g, 3600);
                    Assert.AreEqual(Phase.Over, g.Phase, f + " to " + t + " did not finish"); Assert.AreNotEqual(-1, g.Match.Winner);
                    int ends = 0; LogEntry end = null; foreach (LogEntry e in g.Match.Log) if (!e.IsPoint) { ends++; end = e; }
                    Assert.AreEqual(1, ends);
                    int hi = Math.Max(end.Score[0], end.Score[1]), lo = Math.Min(end.Score[0], end.Score[1]);
                    Assert.IsTrue(hi >= t && hi - lo >= 2 && (hi == t || lo == hi - 2), f + " finished " + end.Score[0] + ":" + end.Score[1]);
                }
        }

        [Test] public void DefaultGamesStayRanked_AndUnsupportedTeamSizesAreRejected()
        {
            foreach (int n in new[] { 1, 2 }) { Game g = new Game(new string[0], n); Assert.AreEqual("ranked", g.Match.Rules.Mode); CollectionAssert.AreEqual(new[] { 25, 25, 15 }, g.Match.Rules.SetTargets); }
            Assert.Throws<ArgumentOutOfRangeException>(() => new Game(new string[0], 3));
        }
    }
}
