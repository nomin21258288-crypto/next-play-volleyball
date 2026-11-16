// C# port of packages/sim-core/src/game.js: players, actions, validation, rules glue.
// NOT COMPILED/RUN in Unity yet (see docs/TEST_RESULTS.md).
using System;
using System.Collections.Generic;

namespace NextPlay.Sim
{
    public enum PlayerAction { None, Serve, Pass, Set, Spike, Block }
    public enum Phase { Serve, Rally, Dead, Over }

    public struct V2 { public double X, Z; public V2(double x, double z) { X = x; Z = z; } }

    public struct PlayerInput
    {
        public bool HasMove; public double MoveX, MoveZ;
        public bool Jump;
        public PlayerAction Action; public double AimZ;
        public static PlayerInput Move(double x, double z) { PlayerInput i = default(PlayerInput); i.HasMove = true; i.MoveX = x; i.MoveZ = z; return i; }
    }

    public sealed class Pending { public PlayerAction A; public double AimZ; public double T; }

    public sealed class Player
    {
        public string Id; public int Team, Idx; public bool Human;
        public double X, Z, Y, Vy, Cd;
        public Pending Pending;
    }

    public sealed class GameEvent
    {
        public double T; public string Type; public string Player; public int Team = -1;
        public string Action; public bool? Ok; public string Reason; public int[] Score; public int[] Sets;
    }

    // Training/tutorial hooks (same fields as JS `coach`). Arrays are indexed by team; null/false = unset.
    public sealed class Coach
    {
        public uint? Seed;
        public V2?[] ServeTarget = new V2?[2];
        public V2?[] PassTarget = new V2?[2];
        public string[] Hitter = new string[2];
        public string[] Blocker = new string[2];
        public bool[] NoBlock = new bool[2];
        public bool NoMiss;
        public double? BlockChance;
    }

    public struct Roll { public bool Block; public double Dig, Aim; }

    public static class Cfg
    {
        public const double Speed = 5.2, JumpV = 4.6, G = 9.81, ReachH = 1.1, ReachUp = 2.3, Cooldown = 0.3, Buffer = 0.3, DeadTime = 1.5;
    }

    public delegate PlayerInput BotFn(Game g, Player p);

    public sealed class Game
    {
        public double T; public List<Player> Players = new List<Player>(); public MatchState Match = new MatchState();
        public Ball Ball; public Phase Phase = Phase.Serve; public double ServeWait, DeadT;
        public List<GameEvent> Events = new List<GameEvent>(); public int LogCursor;
        public PlayerAction LastAction = PlayerAction.None; public int[] ServeIdx = new int[2]; public int PrevServer;
        public Coach Coach; public uint Seed; public Roll Roll = new Roll { Block = true, Dig = 0, Aim = 0 };

        public static readonly double[] BaseX = { 4.5, 4.5 };
        public static readonly double[] BaseZ = { -1.6, 1.6 };
        // 6v6 formation (|x| from the net, z): front row then back row. 1v1 and 2v2 keep BaseX/BaseZ exactly (golden parity).
        public static readonly double[] SixX = { 3.2, 3.2, 3.2, 6.2, 6.2, 6.2 };
        public static readonly double[] SixZ = { -3.0, 0, 3.0, -3.0, 0, 3.0 };
        public static double SlotX(int teamSize, int team, int idx) { return SideSign(team) * (teamSize == 6 ? SixX[idx] : BaseX[idx]); }
        public static double SlotZ(int teamSize, int idx) { return teamSize == 6 ? SixZ[idx] : BaseZ[idx]; }
        public static int SideSign(int team) { return team == 0 ? -1 : 1; }

        public static string ActionName(PlayerAction a) { return a.ToString().ToLowerInvariant(); }

        public Game(string[] humanIds = null, int playersPerTeam = 2, Coach coach = null, MatchRules rules = null)
        {
            if (playersPerTeam != 1 && playersPerTeam != 2 && playersPerTeam != 6) throw new ArgumentOutOfRangeException("playersPerTeam", "must be 1, 2 or 6");
            if (rules != null) Match.Rules = rules;
            Coach = coach ?? new Coach(); Seed = Coach.Seed ?? 12345u;
            humanIds = humanIds ?? new[] { "H0" };
            for (int team = 0; team < 2; team++)
                for (int i = 0; i < playersPerTeam; i++)
                {
                    string id = (team == 0 ? "H" : "O") + i;
                    Players.Add(new Player { Id = id, Team = team, Idx = i, Human = Array.IndexOf(humanIds, id) >= 0, X = SlotX(playersPerTeam, team, i), Z = SlotZ(playersPerTeam, i) });
                }
            Ball = new Ball(new V3(0, 1, 0), new V3(0, 0, 0));
            ResetRally();
        }

        public double Rand() { unchecked { Seed = Seed * 1664525u + 1013904223u; } return Seed / 4294967296.0; }

        public int PlayersPerTeam { get { return TeamSize(); } }
        int TeamSize() { int n = 0; foreach (Player p in Players) if (p.Team == 0) n++; return n; }

        public Player Server()
        {
            foreach (Player p in Players)
                if (p.Team == Match.Server && p.Idx == ServeIdx[Match.Server] % TeamSize()) return p;
            return null;
        }

        public Player Mate(Player p) { foreach (Player q in Players) if (q.Team == p.Team && q != p) return q; return null; }

        void ResetRally()
        {
            Phase = Phase.Serve; ServeWait = 0; LastAction = PlayerAction.None;
            foreach (Player p in Players) { p.X = SlotX(TeamSize(), p.Team, p.Idx); p.Z = SlotZ(TeamSize(), p.Idx); p.Y = 0; p.Vy = 0; p.Pending = null; }
            Player s = Server(); s.X = SideSign(s.Team) * 9.4; s.Z = 0;
            Ball = new Ball(new V3(s.X, 1.1, s.Z), new V3(0, 0, 0));
        }

        void SyncLog()
        {
            while (LogCursor < Match.Log.Count)
            {
                LogEntry e = Match.Log[LogCursor++];
                if (e.IsPoint) Events.Add(new GameEvent { T = T, Type = "point", Team = e.Team, Reason = e.Reason, Score = (int[])e.Score.Clone(), Sets = (int[])Match.Sets.Clone() });
                else Events.Add(new GameEvent { T = T, Type = "set_end", Team = e.Team, Score = e.Score });
            }
        }

        void EndPoint()
        {
            if (Match.Winner != -1) { Phase = Phase.Over; Events.Add(new GameEvent { T = T, Type = "match_over", Team = Match.Winner }); }
            else { Phase = Phase.Dead; DeadT = Cfg.DeadTime; }
        }

        public bool InReach(Player p)
        {
            V3 b = Ball.Pos;
            return Physics.Hyp(b.X - p.X, b.Z - p.Z) <= Cfg.ReachH && b.Y >= 0.1 && b.Y <= p.Y + Cfg.ReachUp;
        }

        static double ClampSide(double v, int team) { return team == 0 ? Math.Min(v, -0.3) : Math.Max(v, 0.3); }
        static double Clamp(double v, double lo, double hi) { return Math.Max(lo, Math.Min(hi, v)); }

        V3 Launch(V3 src, double tx, double tz, double[] times, bool cross)
        {
            PlanResult r = Planner.Plan(src, tx, tz, times, cross);
            if (r == null)
            {
                double[] more = new double[times.Length + 3];
                Array.Copy(times, more, times.Length);
                more[times.Length] = 1.8; more[times.Length + 1] = 2.2; more[times.Length + 2] = 2.6;
                r = Planner.Plan(src, tx, tz, more, cross, 0.2, 1.2);
            }
            if (r != null) return r.V;
            return new V3(-Math.Sign(src.X) * (cross ? 6 : 0), 6, 0);
        }

        static readonly double[] T_SERVE = { 0.9, 1.0, 1.1, 1.3, 1.5 };
        static readonly double[] T_SPIKE = { 0.5, 0.55, 0.6, 0.7, 0.8, 1.0, 1.3, 1.6 };
        static readonly double[] T_FREE = { 1.2, 1.5, 1.8 };
        static readonly double[] T_PASS = { 1.0, 1.2, 1.4, 1.7 };
        static readonly double[] T_SET = { 1.3, 1.4, 1.5, 1.7 };

        // Returns null on success, otherwise a reason string (same strings as the JS reference).
        string DoAction(Player p, PlayerAction a, double aimZ)
        {
            int s = SideSign(p.Team), opp = -s; MatchState m = Match;
            if (Phase == Phase.Serve)
            {
                if (a != PlayerAction.Serve || p != Server()) return "wrong_phase";
                Ball.Pos = new V3(p.X, 2.3, p.Z);
                double tx, tz;
                if (Coach.ServeTarget[p.Team].HasValue) { tx = Coach.ServeTarget[p.Team].Value.X; tz = Coach.ServeTarget[p.Team].Value.Z; }
                else { tx = opp * 5.5; tz = aimZ * 3.2; }
                Ball.Vel = Launch(Ball.Pos, tx, tz, T_SERVE, true);
                Rules.OnServe(m, p.Team, p.Id); Phase = Phase.Rally; LastAction = PlayerAction.Serve;
                double dig = Rand() < 0.75 ? 0 : (Rand() < 0.5 ? -2.2 : 2.2);
                Roll = new Roll { Block = false, Dig = dig, Aim = 0 };
                return null;
            }
            if (Phase != Phase.Rally || a == PlayerAction.Serve) return "wrong_phase";
            if (!InReach(p)) return "out_of_reach";
            V3 bp = Ball.Pos;
            if (a == PlayerAction.Spike && !(p.Y > 0.25 && bp.Y >= 2.0)) return p.Y > 0.25 ? "ball_too_low" : "not_airborne";
            if (a == PlayerAction.Block)
            {
                bool incoming = Math.Sign(Ball.Vel.X) == s && m.Rally.LastTeam != p.Team;
                if (!(p.Y > 0.25 && Math.Abs(p.X) <= 1.6 && Math.Abs(bp.X) <= 1.3 && bp.Y >= 1.8 && incoming)) return "bad_block";
                Rules.OnTouch(m, p.Team, p.Id, true);
                Ball.Vel = new V3(opp * 4.5, -2.5, Ball.Vel.Z * 0.3); LastAction = PlayerAction.Block; return null;
            }
            int logLen = m.Log.Count;
            Rules.OnTouch(m, p.Team, p.Id);
            if (m.Log.Count > logLen) return "fault";
            int touches = m.Rally.Touches; Player mate = Mate(p);
            V3 src = bp; Ball.Pos = src;
            if (a == PlayerAction.Spike)
            {
                double tz = Clamp(aimZ * 3.8, -3.8, 3.8);
                Ball.Vel = Launch(src, opp * 5.0, tz, T_SPIKE, true);
            }
            else if (touches >= 3)
            {
                Ball.Vel = Launch(src, opp * 5.5, 0, T_FREE, true);
            }
            else if (a == PlayerAction.Pass)
            {
                double tx, tz;
                if (Coach.PassTarget[p.Team].HasValue) { tx = Coach.PassTarget[p.Team].Value.X; tz = Coach.PassTarget[p.Team].Value.Z; }
                else if (mate != null) { tx = s * 2.6; tz = Clamp(mate.Z, -3, 3); }
                else { tx = s * 3; tz = 0; }
                Ball.Vel = Launch(src, tx, tz, T_PASS, false);
            }
            else
            {
                Player hit = mate;
                if (Coach.Hitter[p.Team] != null) { hit = null; foreach (Player q in Players) if (q.Id == Coach.Hitter[p.Team]) { hit = q; break; } }
                double tx = s * 1.2, tz = hit != null ? Clamp(hit.Z, -3, 3) : p.Z;
                Ball.Vel = Launch(src, tx, tz, T_SET, false);
            }
            // same order of random draws as the JS object literal: block, dig, aim
            bool blk = Rand() < (Coach.BlockChance ?? 0.55);
            double dg = Rand() < (a == PlayerAction.Spike ? 0.5 : 0.7) ? 0 : (Rand() < 0.5 ? -2.2 : 2.2);
            double am = Rand() * 2 - 1;
            Roll = new Roll { Block = blk, Dig = dg, Aim = am };
            LastAction = a; return null;
        }

        public void Tick(Dictionary<string, PlayerInput> inputs = null, BotFn bot = null)
        {
            if (Phase == Phase.Over) return;
            T += Const.DT;
            foreach (Player p in Players)
            {
                PlayerInput inp = default(PlayerInput);
                if (p.Human) { PlayerInput tmp; if (inputs != null && inputs.TryGetValue(p.Id, out tmp)) inp = tmp; }
                else if (bot != null) inp = bot(this, p);
                p.Cd = Math.Max(0, p.Cd - Const.DT);
                if (inp.HasMove)
                {
                    double L = Physics.Hyp(inp.MoveX, inp.MoveZ); if (L == 0) L = 1;
                    double k = Math.Min(1, L) / L;
                    p.X = ClampSide(p.X + inp.MoveX * k * Cfg.Speed * Const.DT, p.Team);
                    p.Z += inp.MoveZ * k * Cfg.Speed * Const.DT;
                    p.X = Math.Max(-11, Math.Min(11, p.X)); p.Z = Math.Max(-7, Math.Min(7, p.Z));
                }
                if (inp.Jump && p.Y == 0) p.Vy = Cfg.JumpV;
                if (p.Vy != 0 || p.Y > 0) { p.Vy -= Cfg.G * Const.DT; p.Y += p.Vy * Const.DT; if (p.Y <= 0) { p.Y = 0; p.Vy = 0; } }
                if (inp.Action != PlayerAction.None) p.Pending = new Pending { A = inp.Action, AimZ = inp.AimZ, T = Cfg.Buffer };
                if (p.Pending != null)
                {
                    if (p.Cd > 0) { p.Pending.T -= Const.DT; }
                    else
                    {
                        int before = Match.Log.Count;
                        string why = DoAction(p, p.Pending.A, p.Pending.AimZ);
                        if (why == null)
                        {
                            Events.Add(new GameEvent { T = T, Type = "action", Player = p.Id, Team = p.Team, Action = ActionName(p.Pending.A), Ok = true });
                            p.Cd = Cfg.Cooldown; p.Pending = null;
                        }
                        else
                        {
                            p.Pending.T -= Const.DT;
                            if (why == "fault")
                            {
                                if (p.Human) Events.Add(new GameEvent { T = T, Type = "action", Player = p.Id, Team = p.Team, Action = ActionName(p.Pending.A), Ok = false, Reason = "fault" });
                                p.Pending = null;
                            }
                        }
                        if (Match.Log.Count > before) { SyncLog(); EndPoint(); }
                    }
                    if (p.Pending != null && p.Pending.T <= 0)
                    {
                        if (p.Human) Events.Add(new GameEvent { T = T, Type = "action", Player = p.Id, Team = p.Team, Action = ActionName(p.Pending.A), Ok = false });
                        p.Pending = null;
                    }
                }
            }
            if (Phase == Phase.Serve) { ServeWait += Const.DT; Player s = Server(); Ball.Pos = new V3(s.X, 1.1, s.Z); }
            if (Phase == Phase.Rally)
            {
                StepEvent ev = Physics.Step(Ball);
                if ((ev & StepEvent.Net) != 0) Events.Add(new GameEvent { T = T, Type = "net" });
                if (Ball.HasLanded) { Rules.OnLanded(Match, Ball.LandX, Ball.LandZ); SyncLog(); EndPoint(); }
            }
            if (Phase == Phase.Dead)
            {
                DeadT -= Const.DT;
                if (DeadT <= 0)
                {
                    if (Match.Server != PrevServer) { ServeIdx[Match.Server]++; PrevServer = Match.Server; }
                    ResetRally();
                }
            }
        }

        // Reposition for a scripted/training rally (same as JS setupRally).
        public void SetupRally(int serverTeam, int serveIdx = 0)
        {
            Match.Server = serverTeam; ServeIdx[serverTeam] = serveIdx; PrevServer = serverTeam; ResetRally();
        }
    }
}
