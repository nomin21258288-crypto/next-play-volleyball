// C# port of packages/sim-core/src/rules.js. Prototype rules (not final): rally scoring, sets to 25 win-by-2,
// deciding set to 15, best of 3, max 3 touches/side, no consecutive same-player touch, block is not a touch.
using System;
using System.Collections.Generic;

namespace NextPlay.Sim
{
    public sealed class Rally
    {
        public int Touches;
        public int Side = -1;
        public string LastPlayer;
        public int LastTeam = -1;
        public bool Served;
    }

    public sealed class LogEntry
    {
        public int Set;
        public bool IsPoint;
        public int Team;
        public string Reason;
        public int[] Score;
    }

    public sealed class MatchRules
    {
        public const int MinTarget = 7, MaxTarget = 25;
        public string Mode;
        public int SetsToWin;
        public int[] SetTargets;
        public int WinBy = 2;

        public static bool IsValidTarget(int n)
        {
            return n >= MinTarget && n <= MaxTarget;
        }

        public static MatchRules Ranked()
        {
            return new MatchRules
            {
                Mode = "ranked",
                SetsToWin = 2,
                SetTargets = new[] { 25, 25, 15 },
                WinBy = 2
            };
        }

        public static MatchRules Custom(int targetScore, int setsToWin = 1)
        {
            if (!IsValidTarget(targetScore))
                throw new ArgumentOutOfRangeException("targetScore",
                    "custom target must be a whole number from 7 to 25, got " + targetScore);

            if (setsToWin != 1 && setsToWin != 2)
                throw new ArgumentOutOfRangeException("setsToWin",
                    "custom setsToWin must be 1 or 2");

            return new MatchRules
            {
                Mode = "custom",
                SetsToWin = setsToWin,
                SetTargets = new[] { targetScore },
                WinBy = 2
            };
        }

        public int TargetFor(int setIndex)
        {
            return SetTargets[Math.Min(setIndex, SetTargets.Length - 1)];
        }
    }

    public sealed class MatchState
    {
        public MatchRules Rules = MatchRules.Ranked();
        public int[] Sets = new int[2];
        public int[] Score = new int[2];
        public int SetIndex;
        public int Server;
        public Rally Rally = new Rally();
        public int Winner = -1;
        public List<LogEntry> Log = new List<LogEntry>();
    }

    public static class Rules
    {
        static int Target(MatchState m)
        {
            return m.Rules.TargetFor(m.SetIndex);
        }

        static int Other(int t)
        {
            return 1 - t;
        }

        static int SideOfX(double x)
        {
            return x < 0 ? 0 : 1;
        }

        static void Award(MatchState m, int team, string reason)
        {
            m.Score[team]++;

            m.Log.Add(new LogEntry
            {
                Set = m.SetIndex,
                IsPoint = true,
                Team = team,
                Reason = reason,
                Score = new[] { m.Score[0], m.Score[1] }
            });

            int a = m.Score[0], b = m.Score[1], t = Target(m);

            if ((a >= t || b >= t) && Math.Abs(a - b) >= m.Rules.WinBy)
            {
                m.Sets[a > b ? 0 : 1]++;

                m.Log.Add(new LogEntry
                {
                    Set = m.SetIndex,
                    IsPoint = false,
                    Team = a > b ? 0 : 1,
                    Score = new[] { a, b }
                });

                if (m.Sets[0] == m.Rules.SetsToWin ||
                    m.Sets[1] == m.Rules.SetsToWin)
                {
                    m.Winner = m.Sets[0] == m.Rules.SetsToWin ? 0 : 1;
                }
                else
                {
                    m.SetIndex++;
                    m.Score = new int[2];
                }
            }

            m.Server = team;
            m.Rally = new Rally();
        }

        public static void OnServe(MatchState m, int team, string playerId)
        {
            if (m.Winner != -1 || m.Rally.Served) return;

            // FIX: the team that served out of turn loses the point.
            if (team != m.Server)
            {
                Award(m, Other(team), "serve-out-of-turn");
                return;
            }

            m.Rally = new Rally
            {
                Touches = 1,
                Side = team,
                LastPlayer = playerId,
                LastTeam = team,
                Served = true
            };
        }

        public static void OnTouch(MatchState m, int team, string playerId, bool block = false)
        {
            if (m.Winner != -1 || !m.Rally.Served) return;

            Rally r = m.Rally;

            if (block)
            {
                if (r.LastTeam != team)
                {
                    r.Touches = 0;
                    r.Side = team;
                }

                r.LastTeam = team;
                r.LastPlayer = null;
                return;
            }

            if (r.LastTeam != team)
            {
                r.Touches = 0;
                r.Side = team;
                r.LastPlayer = null;
            }

            if (r.LastPlayer == playerId && r.Touches > 0 && r.LastTeam == team)
            {
                Award(m, Other(team), "double-touch");
                return;
            }

            r.Touches++;
            r.LastPlayer = playerId;
            r.LastTeam = team;

            if (r.Touches > 3)
                Award(m, Other(team), "four-touches");
        }

        public static void OnLanded(MatchState m, double x, double z)
        {
            if (m.Winner != -1 || !m.Rally.Served) return;

            bool outside = Math.Abs(x) > Court.HalfLength ||
                           Math.Abs(z) > Court.HalfWidth;

            int landSide = SideOfX(x), last = m.Rally.LastTeam;

            if (outside)
            {
                Award(m, Other(last), "out");
                return;
            }

            Award(m, Other(landSide), "ball-in-court");
        }
    }
}
