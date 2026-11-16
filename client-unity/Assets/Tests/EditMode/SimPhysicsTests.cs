using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NextPlay.Sim;
using NUnit.Framework;

namespace NextPlay.Tests
{
    public class SimPhysicsTests
    {
        static StepEvent RunBall(Ball b, int max = 20000) { StepEvent all = StepEvent.None; for (int i = 0; i < max && !b.HasLanded; i++) all |= Physics.Step(b); return all; }

        [Test] public void BallFallsUnderGravity()
        {
            Ball b = new Ball(new V3(5, 3, 0), new V3(0, 0, 0)); double t = 0;
            while (!b.HasLanded) { Physics.Step(b); t += Const.DT; }
            Assert.That(t, Is.EqualTo(System.Math.Sqrt(2 * 2.895 / 9.81)).Within(0.03));
        }
        [Test] public void Deterministic()
        {
            Ball a = new Ball(new V3(-8, 2, 1), new V3(14, 6, -1)), b = new Ball(new V3(-8, 2, 1), new V3(14, 6, -1));
            RunBall(a); RunBall(b);
            Assert.AreEqual(a.LandX, b.LandX); Assert.AreEqual(a.LandZ, b.LandZ);
        }
        [Test] public void LowBallHitsNetAndStaysOnItsSide()
        {
            Ball b = new Ball(new V3(-1, 1.2, 0), new V3(10, 1, 0)); StepEvent ev = RunBall(b);
            Assert.IsTrue((ev & StepEvent.Net) != 0); Assert.Less(b.LandX, 0);
        }
        [Test] public void HighBallClearsNet()
        {
            Ball b = new Ball(new V3(-6, 2.5, 0), new V3(9, 6, 0)); RunBall(b); Assert.Greater(b.LandX, 0);
        }
        [Test] public void LandedBallDoesNotMove()
        {
            Ball b = new Ball(new V3(0, 0.2, 3), new V3(0, -3, 0)); RunBall(b); V3 p = b.Pos;
            Assert.AreEqual(StepEvent.None, Physics.Step(b)); Assert.AreEqual(p.X, b.Pos.X); Assert.AreEqual(p.Y, b.Pos.Y);
        }

        // ---- parity with the JS reference (golden.json) ----
        [Test] public void Golden_BallPhysics()
        {
            JArray arr = (JArray)TestUtil.Golden()["physics"]; int idx = 0;
            foreach (JToken c in arr)
            {
                Ball b = new Ball(TestUtil.ToV3(c["pos"]), TestUtil.ToV3(c["vel"])); List<string> ev = new List<string>(); int n = 0;
                while (!b.HasLanded && n++ < 20000) { StepEvent e = Physics.Step(b); if ((e & StepEvent.Net) != 0) ev.Add("net"); if ((e & StepEvent.Landed) != 0) ev.Add("landed"); }
                CollectionAssert.AreEqual(c["events"].ToObject<List<string>>(), ev, "events case " + idx);
                Assert.AreEqual((int)c["steps"], n, "steps case " + idx);
                Assert.That(b.LandX, Is.EqualTo((double)c["landed"]["x"]).Within(TestUtil.Tol), "landX " + idx);
                Assert.That(b.LandZ, Is.EqualTo((double)c["landed"]["z"]).Within(TestUtil.Tol), "landZ " + idx);
                Assert.That(b.Vel.X, Is.EqualTo((double)c["endVel"]["x"]).Within(TestUtil.Tol), "velX " + idx);
                idx++;
            }
        }
        [Test] public void Golden_Simulate()
        {
            foreach (JToken c in (JArray)TestUtil.Golden()["sims"])
            {
                double? stop = c["stopY"].Type == JTokenType.Null ? (double?)null : (double)c["stopY"];
                SimResult r = Planner.Simulate(TestUtil.ToV3(c["pos"]), TestUtil.ToV3(c["vel"]), 6, stop); JToken e = c["result"];
                Assert.That(r.X, Is.EqualTo((double)e["x"]).Within(TestUtil.Tol)); Assert.That(r.Y, Is.EqualTo((double)e["y"]).Within(TestUtil.Tol));
                Assert.That(r.Z, Is.EqualTo((double)e["z"]).Within(TestUtil.Tol)); Assert.That(r.T, Is.EqualTo((double)e["t"]).Within(TestUtil.Tol));
                Assert.AreEqual((bool)e["net"], r.Net); Assert.AreEqual(e["landed"].Type != JTokenType.Null, r.Landed);
                Assert.AreEqual(e["crossY"].Type != JTokenType.Null, r.HasCrossY);
                if (r.HasCrossY) Assert.That(r.CrossY, Is.EqualTo((double)e["crossY"]).Within(TestUtil.Tol));
            }
        }
        [Test] public void Golden_Planner()
        {
            foreach (JToken c in (JArray)TestUtil.Golden()["plans"])
            {
                V3 start = TestUtil.ToV3(c["from"]); double[] times = c["times"].ToObject<double[]>();
                PlanResult r = Planner.Plan(start, (double)c["target"]["x"], (double)c["target"]["z"], times, (bool)c["cross"]); JToken e = c["result"];
                Assert.AreEqual(e.Type != JTokenType.Null, r != null);
                if (r == null) continue;
                Assert.That(r.V.X, Is.EqualTo((double)e["v"]["x"]).Within(TestUtil.Tol)); Assert.That(r.V.Y, Is.EqualTo((double)e["v"]["y"]).Within(TestUtil.Tol));
                Assert.That(r.V.Z, Is.EqualTo((double)e["v"]["z"]).Within(TestUtil.Tol)); Assert.That(r.T, Is.EqualTo((double)e["T"]).Within(TestUtil.Tol));
            }
        }
    }
}
