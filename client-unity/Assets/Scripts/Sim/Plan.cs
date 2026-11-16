// C# port of packages/sim-core/src/plan.js (ballistic shot planner using the real physics step).
using System;

namespace NextPlay.Sim
{
    public struct SimResult
    {
        public double X, Y, Z, T;
        public bool HasCrossY; public double CrossY;
        public bool Net, Landed;
        public double LandX, LandZ;
    }

    public sealed class PlanResult
    {
        public V3 V; public double T;
    }

    public static class Planner
    {
        public static SimResult Simulate(V3 pos, V3 vel, double maxT = 6, double? stopY = null)
        {
            Ball b = new Ball(pos, vel);
            double t = 0; bool hasCross = false; double crossY = 0; bool net = false;
            while (!b.HasLanded && t < maxT)
            {
                double px = b.Pos.X;
                StepEvent ev = Physics.Step(b); t += Const.DT;
                if ((ev & StepEvent.Net) != 0) { net = true; break; }
                if (px * b.Pos.X < 0) { hasCross = true; crossY = b.Pos.Y; }
                if (stopY.HasValue && b.Vel.Y < 0 && b.Pos.Y <= stopY.Value)
                    return new SimResult { X = b.Pos.X, Y = b.Pos.Y, Z = b.Pos.Z, T = t, HasCrossY = hasCross, CrossY = crossY, Net = net, Landed = false };
            }
            return new SimResult { X = b.Pos.X, Y = b.Pos.Y, Z = b.Pos.Z, T = t, HasCrossY = hasCross, CrossY = crossY, Net = net, Landed = b.HasLanded, LandX = b.LandX, LandZ = b.LandZ };
        }

        // target is (x, z). Returns null if no solution (same as JS returning null).
        public static PlanResult Plan(V3 start, double targetX, double targetZ, double[] times, bool cross, double clearance = 0.2, double tol = 0.5)
        {
            foreach (double T in times)
            {
                double vx = (targetX - start.X) / T, vz = (targetZ - start.Z) / T;
                double vy = (BallCfg.Radius - start.Y + 0.5 * BallCfg.Gravity * T * T) / T;
                SimResult r = default(SimResult);
                for (int i = 0; i < 6; i++)
                {
                    r = Simulate(start, new V3(vx, vy, vz));
                    if (r.Net || !r.Landed) break;
                    vx += (targetX - r.LandX) / r.T; vz += (targetZ - r.LandZ) / r.T;
                }
                if (r.Net || !r.Landed) continue;
                double err = Physics.Hyp(r.LandX - targetX, r.LandZ - targetZ);
                bool crossed = r.HasCrossY;
                if (err > tol) continue;
                if (cross && !(crossed && r.CrossY >= Court.NetHeight + BallCfg.Radius + clearance)) continue;
                if (!cross && crossed) continue;
                return new PlanResult { V = new V3(vx, vy, vz), T = T };
            }
            return null;
        }
    }
}
