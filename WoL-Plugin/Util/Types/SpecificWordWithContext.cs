using System;

namespace WoLightning.WoL_Plugin.Util.Types
{
    // todo: this naming is kinda stupid, maybe refactor
    public class SpecificWordWithContext : SpecificWord
    {
        public string MatchPreWord = "";
        public string MatchPostWord = "";
        public bool NeedsDirectLinking = false;

        public SpecificWordWithContext(string Word) : base(Word)
        {
        }



        public bool MatchesContext(string EntireText)
        {
            string EntireText_ = EntireText;
            string MatchPreWord_ = MatchPreWord;
            string MatchPostWord_ = MatchPostWord;
            string Word_ = Word;

            if (!NeedsProperCase)
            {
                EntireText_ = EntireText_.ToLower();
                MatchPreWord_ = MatchPreWord_.ToLower();
                MatchPostWord_ = MatchPostWord_.ToLower();
                //Word_ = Word_.ToLower();
            }

            int preIndex = -2;
            int postIndex = -2;

            if (MatchPreWord_.Length > 0)
            {
                preIndex = EntireText_.IndexOf(MatchPreWord_);
                if (preIndex == -1) return false;
            }

            if (MatchPostWord_.Length > 0)
            {
                postIndex = EntireText_.IndexOf(MatchPostWord_);
                if (postIndex == -1) return false;
            }

            if (preIndex != -2 && postIndex != -2 && preIndex >= postIndex) return false;

            if (NeedsDirectLinking)
            {
                // todo
            }

            return true;
        }

        public bool IsContextFulfilled(string EntireText)
        {

            string WordCompound = "";

            if (MatchPreWord.Length > 0)
                WordCompound += MatchPreWord;

            WordCompound += Word;

            if (MatchPostWord.Length > 0)
                WordCompound += MatchPostWord;

            if (!NeedsPunctuation) return EntireText.Contains(WordCompound);

            return EntireText.Equals(WordCompound);
        }

        public override String ToString()
        {
            return MatchPreWord + " " + Word + " " + MatchPostWord;
        }
    }
}
