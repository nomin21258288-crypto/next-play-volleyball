// The six-character roster (config/characters.json). Pure C#. Rules come from the user's handoff:
// exactly six people, order 1..6, each linked to its OWN source photo, no jersey numbers, role labels only PROPOSED.
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextPlay.Core
{
    public sealed class CharacterDef
    {
        public string id, sourceKey, roleLabel, source, faceTexture, hairColor, skinColor;
        public int order; public int[] cropBox;
    }

    public sealed class CharacterRoster
    {
        public int version; public string note;
        public bool roleMappingApproved, jerseyNumbersApproved;
        public List<CharacterDef> characters = new List<CharacterDef>();

        public static readonly string[] AllowedRoles = { "Smasher", "All-Rounder", "Defender", "Blocker", "Setter", "Server" };
        public static readonly string[] ExpectedSources =
        {
            "01_main_front_reference.png", "02_second_position_reference.png",
            "04A_two_person_photo_left_player_crop.jpg", "04B_two_person_photo_right_player_crop.jpg",
            "05_cream_kurta_reference.png", "06_circular_portrait_reference.png"
        };

        public static CharacterRoster Parse(string json)
        {
            CharacterRoster r = JsonConvert.DeserializeObject<CharacterRoster>(json);
            if (r == null) throw new FormatException("characters json is empty");
            r.characters.Sort((a, b) => a.order.CompareTo(b.order));
            return r;
        }

        public static bool IsHexColor(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length != 7 || s[0] != '#') return false;
            for (int i = 1; i < 7; i++) if (Uri.IsHexDigit(s[i]) == false) return false;
            return true;
        }

        public List<string> Validate()
        {
            List<string> e = new List<string>();
            if (characters.Count != 6) { e.Add("expected exactly 6 characters, found " + characters.Count); return e; }
            HashSet<string> tex = new HashSet<string>(), src = new HashSet<string>(), roles = new HashSet<string>(), ids = new HashSet<string>();
            for (int i = 0; i < 6; i++)
            {
                CharacterDef c = characters[i];
                if (c.order != i + 1) e.Add("character " + c.id + " has order " + c.order + ", expected " + (i + 1));
                if (c.source != ExpectedSources[i]) e.Add("character " + (i + 1) + " must use source " + ExpectedSources[i] + " but uses " + c.source);
                if (!ids.Add(c.id)) e.Add("duplicate id " + c.id);
                if (!tex.Add(c.faceTexture)) e.Add("duplicate face texture " + c.faceTexture);
                if (!src.Add(c.source)) e.Add("duplicate source photo " + c.source);
                if (Array.IndexOf(AllowedRoles, c.roleLabel) < 0) e.Add("unknown role label " + c.roleLabel);
                if (!roles.Add(c.roleLabel)) e.Add("duplicate role label " + c.roleLabel);
                if (c.cropBox == null || c.cropBox.Length != 4 || c.cropBox[2] <= c.cropBox[0] || c.cropBox[3] <= c.cropBox[1]) e.Add("bad cropBox for " + c.id);
                if (!IsHexColor(c.hairColor) || !IsHexColor(c.skinColor)) e.Add("bad hair/skin colour for " + c.id);
            }
            if (jerseyNumbersApproved) e.Add("jerseyNumbersApproved must stay false until the user supplies real numbers");
            return e;
        }
    }
}
