// C# port of packages/sim-core/src/bot.js: serve, receive/pass, set, spike, block, reposition.
using System;
using System.Collections.Generic;

namespace NextPlay.Sim
{
    public static class Bot
    {
        const double SpeedStep = 0.06;

        static PlayerInput Toward(Player p, double tx, double tz)
        {
            double dx = tx - p.X, dz = tz - p.Z, d = Physics.Hyp(dx, dz);
            if (d < SpeedStep) return PlayerInput.Move(0, 0);
            double k = Math.Min(1, d / 0.3);
            return PlayerInput.Move(dx / d * k, dz / d * k);
        }

        static double Clamp(double v, double lo, double hi) { return Math.Max(lo, Math.Min(hi, v)); }

        // total order used by the JS sort comparators: primary key, then player idx
        static Player MinBy(List<Player> list, Func<Player, double> key)
        {
            Player best = null; double bestK = 0;
            foreach (Player q in list)
            {
                double k = key(q);
                if (best == null) { best = q; bestK = k; continue; }
                double diff = k - bestK;
                if (diff < 0 || (diff == 0 && q.Idx < best.Idx)) { best = q; bestK = k; }
            }
            return best;
        }

        public static PlayerInput Input(Game g, Player p)
        {
            int s = Game.SideSign(p.Team); MatchState m = g.Match; Ball ball = g.Ball;
            double baseX = Game.SlotX(g.PlayersPerTeam, p.Team, p.Idx), baseZ = Game.SlotZ(g.PlayersPerTeam, p.Idx);
            if (g.Phase == Phase.Serve)
            {
                if (p != g.Server()) return Toward(p, baseX, baseZ);
                double spotX = s * 9.4;
                bool at = Physics.Hyp(p.X - spotX, p.Z - 0) < 0.1;
                double aim = (((m.Score[0] + m.Score[1]) % 3) - 1) * 0.6;
                if (at && g.ServeWait > 1.0) { PlayerInput o = default(PlayerInput); o.Action = PlayerAction.Serve; o.AimZ = aim; return o; }
                return Toward(p, spotX, 0);
            }
            if (g.Phase != Phase.Rally) return Toward(p, baseX, baseZ);

            bool lastIsMine = m.Rally.LastTeam == p.Team;
            int touchesMine = lastIsMine ? m.Rally.Touches : 0;
            bool incoming = Math.Sign(ball.Vel.X) == s;
            bool oppAttack = !lastIsMine && g.LastAction == PlayerAction.Spike;
            List<Player> team = g.Players.FindAll(q => q.Team == p.Team);

            // ---- block: pre-position while the set is in the air, jump when the spike is in flight ----
            bool oppSet = !lastIsMine && m.Rally.LastTeam != -1 && m.Rally.Touches == 2 && g.LastAction == PlayerAction.Set;
            bool noBlock = g.Coach.NoBlock[p.Team];
            if (((oppAttack && incoming && Math.Abs(ball.Pos.X) > 0.3) || oppSet) && team.Count > 1 && !noBlock)
            {
                double zAtNet = oppAttack
                    ? ball.Pos.Z + ball.Vel.Z * (Math.Abs(ball.Pos.X) / Math.Max(0.1, Math.Abs(ball.Vel.X)))
                    : Planner.Simulate(ball.Pos, ball.Vel, 6, 2.9).Z;
                Player forced = null;
                if (g.Coach.Blocker[p.Team] != null) foreach (Player q in team) if (q.Id == g.Coach.Blocker[p.Team]) { forced = q; break; }
                Player blocker = forced ?? MinBy(team, q => Math.Abs(q.Z - zAtNet));
                if (p == blocker)
                {
                    PlayerInput o = Toward(p, s * 0.8, Clamp(zAtNet, -4, 4));
                    if (oppAttack && g.Roll.Block)
                    {
                        double tNet = Math.Abs(ball.Pos.X) / Math.Max(0.1, Math.Abs(ball.Vel.X));
                        if (tNet < 0.3 && p.Y == 0) o.Jump = true;
                        if (p.Y > 0.25) o.Action = PlayerAction.Block;
                    }
                    return o;
                }
            }

            // ---- who plays the ball ----
            double wantsY = touchesMine == 2 ? 2.9 : 1.0;
            SimResult pred = Planner.Simulate(ball.Pos, ball.Vel, 6, wantsY);
            double predSignSrc = pred.X != 0 ? pred.X : s;
            bool mySide = Math.Sign(predSignSrc) == s || Math.Abs(pred.X) < 0.5;
            bool canPlay = mySide && (!pred.Landed || Math.Abs(pred.X) <= 11) && (lastIsMine ? m.Rally.Touches < 3 : true);
            if (canPlay)
            {
                List<Player> cands = team.FindAll(q => !(lastIsMine && m.Rally.Touches > 0 && m.Rally.LastPlayer == q.Id));
                Player who = MinBy(cands, q => Physics.Hyp(q.X - pred.X, q.Z - pred.Z));
                if (who == p)
                {
                    double d = Physics.Hyp(ball.Pos.X - p.X, ball.Pos.Z - p.Z);
                    double miss = (!lastIsMine && !g.Coach.NoMiss) ? g.Roll.Dig : 0;
                    PlayerInput o = Toward(p, Clamp(pred.X, -11, 11), pred.Z + miss);
                    if (touchesMine == 2)
                    {
                        if (d < 2.2 && ball.Pos.Y < 3.8 && ball.Vel.Y < 0.5 && Math.Abs(ball.Pos.X) <= 3.2 && p.Y == 0) o.Jump = true;
                        if (p.Y > 0.25 && ball.Vel.Y <= 0.5 && ball.Pos.Y >= 2.3 && Math.Abs(ball.Pos.X) <= 3.0) o.Action = PlayerAction.Spike;
                        else if (ball.Pos.Y < 1.3 && d < 1.0) o.Action = PlayerAction.Pass;
                        o.AimZ = g.Roll.Aim;
                    }
                    else if (d < 1.0) o.Action = touchesMine == 1 ? PlayerAction.Set : PlayerAction.Pass;
                    return o;
                }
            }
            // supporting positions
            if (lastIsMine && touchesMine == 1) return Toward(p, s * 1.8, p.Z);
            return Toward(p, baseX, baseZ);
        }
    }
}
