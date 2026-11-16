using NextPlay.Sim;
using NUnit.Framework;

namespace NextPlay.Tests
{
    public class SimRulesTests
    {
        [Test] public void ServeCrossesAndLandsInCourt_ServerScoresAndServesAgain()
        {
            MatchState m = new MatchState(); Rules.OnServe(m, 0, "A1"); Rules.OnLanded(m, 5, 0);
            Assert.AreEqual(1, m.Score[0]); Assert.AreEqual(0, m.Score[1]); Assert.AreEqual(0, m.Server);
        }
        [Test] public void OutBallFaultsLastToucher()
        {
            MatchState m = new MatchState(); Rules.OnServe(m, 0, "A1"); Rules.OnLanded(m, 12, 0);
            Assert.AreEqual(0, m.Score[0]); Assert.AreEqual(1, m.Score[1]); Assert.AreEqual(1, m.Server);
        }
        [Test] public void LandingOnOwnSideLosesTheRally()
        {
            MatchState m = new MatchState(); Rules.OnServe(m, 0, "A1"); Rules.OnLanded(m, -5, 0);
            Assert.AreEqual(1, m.Score[1]);
        }
        [Test] public void FourTouchesIsAFault()
        {
            MatchState m = new MatchState(); Rules.OnServe(m, 0, "A1"); Rules.OnTouch(m, 0, "A2"); Rules.OnTouch(m, 0, "A3"); Rules.OnTouch(m, 0, "A4");
            Assert.AreEqual(1, m.Score[1]);
        }
        [Test] public void DoubleTouchIsAFault()
        {
            MatchState m = new MatchState(); Rules.OnServe(m, 0, "A1"); Rules.OnTouch(m, 0, "A1"); Assert.AreEqual(1, m.Score[1]);
        }
        [Test] public void BlockIsNotATouch()
        {
            MatchState m = new MatchState(); Rules.OnServe(m, 0, "A1");
            Rules.OnTouch(m, 1, "B1"); Rules.OnTouch(m, 1, "B2", true);       // B1 touches, B2 blocks (not counted)
            Assert.AreEqual(1, m.Rally.Touches); Assert.IsNull(m.Rally.LastPlayer);
            Rules.OnTouch(m, 1, "B1"); Assert.AreEqual(2, m.Rally.Touches);    // B1 may play again after a block
            Assert.AreEqual(0, m.Score[0] + m.Score[1]);
        }
        [Test] public void ServeOutOfTurnLosesPoint()
        {
            MatchState m = new MatchState(); Rules.OnServe(m, 1, "B1"); Assert.AreEqual(1, m.Score[0]);
        }
        [Test] public void SetEndsAt25WinBy2_MatchAtTwoSets_DecidingSetTo15()
        {
            MatchState m = new MatchState();
            System.Action<int> win = t => { Rules.OnServe(m, m.Server, "S"); Rules.OnLanded(m, t == 0 ? 5 : -5, 0); };
            for (int i = 0; i < 24; i++) { win(0); win(1); }
            win(0); Assert.AreEqual(0, m.Sets[0]);                  // 25-24: not enough
            win(0); Assert.AreEqual(1, m.Sets[0]); Assert.AreEqual(1, m.SetIndex);   // 26-24
            for (int i = 0; i < 25; i++) win(1);                     // team 1 wins set 2 (25-0)
            Assert.AreEqual(1, m.Sets[1]); Assert.AreEqual(2, m.SetIndex);
            for (int i = 0; i < 14; i++) { win(0); win(1); }         // 14-14
            win(0); Assert.AreEqual(-1, m.Winner);                   // 15-14: not enough
            win(0); Assert.AreEqual(0, m.Winner);                    // 16-14 in the deciding set (target 15)
        }
        [Test] public void NoScoringAfterMatchOver()
        {
            MatchState m = new MatchState(); m.Winner = 0; m.Rally.Served = true; Rules.OnLanded(m, 5, 0); Assert.AreEqual(0, m.Score[0]);
        }
    }
}
