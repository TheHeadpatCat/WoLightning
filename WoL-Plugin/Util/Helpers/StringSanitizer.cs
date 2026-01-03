namespace WoLightning.Util
{
    public static class StringSanitizer
    {
        public static string LetterOrDigit(string s)
        {
            string output = string.Empty;
            foreach (char c in s.ToCharArray())
            {
                if (c != ' ' && !char.IsLetterOrDigit(c)) continue;
                output += c;
            }
            return output;
        }

        public static string PlayerName(string s) //todo implement
        {
            string output = string.Empty;
            foreach (char c in s.ToCharArray())
            {
                if (c != ' ' && c != '\'' && c!= '-' && !char.IsLetter(c)) continue;
                output += c;
            }
            return output;
        }
    }
}
