using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;

namespace WoLightning.WoL_Plugin.Util
{
    public class GameEmotes
    {
        private Plugin plugin;
        private IReadOnlyDictionary<uint, Emote> emotes;

        public GameEmotes(Plugin plugin, IReadOnlyDictionary<uint, Emote> emotes)
        {
            this.plugin = plugin;
            this.emotes = emotes;
        }

        public List<Emote>? findEmotesByName(String Name)
        {
            List<Emote>? output = [];

            foreach (var (id, emote) in emotes)
            {
                if (output.Count >= 14) break;
                string EmoteName = emote.Name.ToString().ToLower();
                if (EmoteName.Equals(Name.ToLower()) && !output.Contains(emote)) output.Add(emote);
                if (EmoteName.Contains(Name.ToLower()) && !output.Contains(emote)) output.Add(emote);
            }

            return output;
        }
        public List<Emote>? findEmotes(List<ushort> ids)
        {
            List<Emote>? output = [];

            foreach (var id in ids)
            {
                Emote? s = getEmote(id);
                if (s != null) output.Add((Emote)s);
            }

            return output;
        }

        public Emote? getEmote(String Name)
        {
            Emote? output = null;
            foreach (var (id, emote) in emotes)
            {
                string EmoteName = emote.Name.ToString().ToLower();
                if (EmoteName.Equals(Name.ToLower()))
                {
                    output = emote;
                    break;
                }
                if (output == null && EmoteName.Contains(Name.ToLower())) output = emote;
            }
            return output;
        }
        public Emote? getEmote(uint Id)
        {
            Emote? output = null;
            foreach (var (Eid, emote) in emotes)
            {
                if (Eid == Id)
                {
                    output = emote;
                    break;
                }

            }
            return output;
        }
    }

}
