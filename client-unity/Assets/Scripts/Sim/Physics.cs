// C# port of packages/sim-core/src/physics.js. NOT COMPILED/RUN in Unity yet (see docs/TEST_RESULTS.md).
// Pure C# (no UnityEngine) and `double` precision so it can reproduce the JS reference bit-for-bit.
using System;

namespace NextPlay.Sim
{
    public struct V3
    {
        public double X, Y, Z;
        public V3(double x, double y, double z) { X = x; Y = y; Z = z; }
    }

    public static class Court
    {
        public const double HalfLength = 9, HalfWidth = 4.5, NetHeight = 2.43;
    }

    public static class BallCfg
    {
        public const double Radius = 0.105, Gravity = 9.81, Drag = 0.02, NetDamping = 0.3;
    }

    public static class Const
    {
        public const double DT = 1.0 / 120;
    }

    public sealed class Ball
    {
        public V3 Pos, Vel;
        public bool HasLanded;
        public double LandX, LandZ;
        public Ball(V3 pos, V3 vel) { Pos = pos; Vel = vel; }
    }

    [Flags]
    public enum StepEvent { None = 0, Net = 1, Landed = 2 }

    public static class Physics
    {
        public static double Hyp(double a, double b) { return Math.Sqrt(a * a + b * b); }

        // Advance one fixed step (identical operation order to the JS reference).
        public static StepEvent Step(Ball ball)
        {
            if (ball.HasLanded) return StepEvent.None;
            StepEvent ev = StepEvent.None;
            double prevX = ball.Pos.X;
            ball.Vel.Y -= BallCfg.Gravity * Const.DT;
            double k = 1 - BallCfg.Drag * Const.DT;
            ball.Vel.X *= k; ball.Vel.Z *= k;
            ball.Pos.X += ball.Vel.X * Const.DT;
            ball.Pos.Y += ball.Vel.Y * Const.DT;
            ball.Pos.Z += ball.Vel.Z * Const.DT;
            if (prevX * ball.Pos.X < 0 && ball.Pos.Y - BallCfg.Radius < Court.NetHeight && Math.Abs(ball.Pos.Z) <= Court.HalfWidth + 0.5)
            {
                ball.Pos.X = prevX; ball.Vel.X = -ball.Vel.X * BallCfg.NetDamping; ev |= StepEvent.Net;
            }
            if (ball.Pos.Y <= BallCfg.Radius)
            {
                ball.Pos.Y = BallCfg.Radius;
                ball.HasLanded = true; ball.LandX = ball.Pos.X; ball.LandZ = ball.Pos.Z;
                ev |= StepEvent.Landed;
            }
            return ev;
        }
    }
}
