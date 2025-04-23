using System;

namespace WoLightning.WoL_Plugin.Util.Types
{

    public enum WordPosition
    {
        Any,
        First,
        Last,
        Inbetween
    }

    public class SpecificWord
    {

        public String Word;
        public bool NeedsProperCase = false;
        public bool NeedsPunctuation = false;
        public WordPosition NeedsPosition = WordPosition.Any;

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

            return ThisWord.Equals(OtherWord);
        }

        public override String ToString()
        {
            return this.Word;
        }
    }
}
