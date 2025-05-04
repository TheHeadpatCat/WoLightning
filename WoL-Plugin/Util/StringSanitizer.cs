namespace WoLightning.Util
{
    public class StringSanitizer
    {
        public static string LetterOrDigit(string s)
        {
            string output = string.Empty;
            foreach (char c in s.ToCharArray())
            {
                if (c != ' ' && c != '\'' && !char.IsLetterOrDigit(c)) continue;
                output += c;
            }
            return output;
        }

        public static string PlayerName(string s) //todo implement
        {
            return string.Empty;
        }
    }
}
