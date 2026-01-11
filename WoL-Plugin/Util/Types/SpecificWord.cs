using System;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Util.Types
{

    public enum WordPosition
    {
        Any,
        First,
        Last,
        Inbetween
    }

    [Serializable]
    public class SpecificWord
    {

        public String Word { get; set; } = "";
        public bool NeedsProperCase { get; set; } = false;
        public bool NeedsPunctuation { get; set; } = false;
        public WordPosition NeedsPosition { get; set; } = WordPosition.Any;
        public DeviceOptions ShockOptions { get; set; } = new();

        public SpecificWord(String Word) { this.Word = Word; }

        public bool Compare(String word)
        {
            String ThisWord = this.Word;
            String OtherWord = word;

            if (!NeedsProperCase)
            {
                OtherWord = OtherWord.ToLower();
                ThisWord = ThisWord.ToLower();
            }

            if (!NeedsPunctuation) return OtherWord.Contains(ThisWord);

            return ThisWord.Equals(OtherWord);
        }

        public override String ToString()
        {
            return this.Word;
        }
    }
}
