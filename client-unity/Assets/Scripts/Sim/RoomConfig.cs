// Create Room settings (C# port of packages/sim-core/src/room.js). Pure C#.
// Only the creator of a CUSTOM room can choose the target score (any whole number 7..25). Ranked rooms use fixed official scoring.
using System;
using System.Collections.Generic;

namespace NextPlay.Sim
{
    public sealed class RoomConfig
    {
        public string CreatorId, Format, Mode;
        public int PlayersPerTeam;
        public MatchRules Rules;
        public bool Started;

        public static readonly string[] Formats = { "1v1", "2v2", "6v6" };
        public static int PlayersFor(string format)
        {
            switch (format) { case "1v1": return 1; case "2v2": return 2; case "6v6": return 6; default: throw new ArgumentOutOfRangeException("format", "format must be 1v1, 2v2 or 6v6"); }
        }
        public static List<int> ScoreOptions()
        {
            List<int> l = new List<int>(); for (int n = MatchRules.MinTarget; n <= MatchRules.MaxTarget; n++) l.Add(n); return l;
        }

        // mode "custom": targetScore is required (7..25). mode "ranked": targetScore must be null.
        public static RoomConfig Create(string creatorId, string format, string mode = "custom", int? targetScore = null, int setsToWin = 1)
        {
            if (string.IsNullOrEmpty(creatorId)) throw new ArgumentException("creatorId is required");
            int n = PlayersFor(format);
            MatchRules rules;
            if (mode == "ranked")
            {
                if (targetScore.HasValue) throw new ArgumentException("ranked matches use fixed official scoring: targetScore is not allowed");
                rules = MatchRules.Ranked();
            }
            else if (mode == "custom")
            {
                if (!targetScore.HasValue) throw new ArgumentOutOfRangeException("targetScore", "custom rooms must choose a target score from 7 to 25");
                rules = MatchRules.Custom(targetScore.Value, setsToWin);
            }
            else throw new ArgumentOutOfRangeException("mode", "unknown match mode " + mode);
            return new RoomConfig { CreatorId = creatorId, Format = format, Mode = mode, PlayersPerTeam = n, Rules = rules };
        }

        // Returns a NEW room. Only the creator, only a custom room, only before the match has started.
        public RoomConfig WithTargetScore(string actorId, int targetScore)
        {
            if (Started) throw new InvalidOperationException("the match has already started");
            if (Mode != "custom") throw new InvalidOperationException("ranked rooms use fixed official scoring");
            if (actorId != CreatorId) throw new InvalidOperationException("only the room creator can change the target score");
            return new RoomConfig { CreatorId = CreatorId, Format = Format, Mode = Mode, PlayersPerTeam = PlayersPerTeam, Started = false, Rules = MatchRules.Custom(targetScore, Rules.SetsToWin) };
        }

        // The room's rules drive the real simulation; scoreboard reads game.Match.Rules.
        public Game StartGame(string[] humanIds = null, Coach coach = null) { return new Game(humanIds ?? new[] { "H0" }, PlayersPerTeam, coach, Rules); }
    }
}
