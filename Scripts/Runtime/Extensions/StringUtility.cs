namespace BrunoMikoski.Core.DevelopmentTools
{
    public static partial class StringUtility
    {
        private const string HungarianPrefix = "m_";
        private const string Underscore = "_";
        private const string HumanReadableSeparator = " ";


        /// <summary>
        /// Gets the human readable version of  programmer text like a variable name.
        /// </summary>
        /// <param name="programmerText">The programmer text.</param>
        /// <returns>The human readable equivalent of the programmer text.</returns>
        public static string GetHumanReadableText(string programmerText)
        {
            bool wasLetter = false;
            bool wasUpperCase = false;
            bool addedSpace = false;
            string result = "";

            // First remove the  prefix if it exists.
            if (programmerText.StartsWith(HungarianPrefix))
                programmerText = programmerText.Substring(HungarianPrefix.Length);

            // Deal with any miscellanneous underscores.
            programmerText = programmerText.Replace(Underscore, string.Empty);

            // Go through the original string and copy it with some modifications.
            for (int i = 0; i < programmerText.Length; i++)
            {
                // If we try to convert "iOS" to a human readable string, this method would
                // return something like "i Os". So we check if we find the text "iOS" here,
                // and just copy it to the result.
                if (MatchesIOS(programmerText, i))
                {
                    result += "iOS";
                    wasLetter = true;
                    wasUpperCase = false;
                    i += 3;
                    continue;
                }

                // If there was a change in caps add spaces.
                bool isNumberOrUpper = char.IsUpper(programmerText[i]) || char.IsNumber(programmerText[i]);
                if ((isNumberOrUpper != wasUpperCase)
                    && i > 0 && !addedSpace)
                {
                    // Upper case to lower case.
                    // I added this so that something like 'GUIItem' turns into 'GUI Item',
                    // but that means we have to make sure that no symbols are involved.
                    if (wasUpperCase && i > 1)
                    {
                        // From letter to letter means we have to insert a space one character back.
                        // Otherwise it's going from a letter to a symbol and we can just add a space.
                        if (wasLetter && char.IsLetter(programmerText[i]))
                            result = result.Insert(result.Length - 1, HumanReadableSeparator);
                        else
                            result += HumanReadableSeparator;
                        addedSpace = true;
                    }

                    // Lower case to upper case.
                    if (!wasUpperCase)
                    {
                        result += HumanReadableSeparator;
                        addedSpace = true;
                    }
                }
                else
                {
                    // No case change.
                    addedSpace = false;
                }

                // Add the character.
                result += programmerText[i];

                // Capitalize the first character.
                if (i == 0)
                    result = result.ToUpper();

                // Remember things about the previous letter.
                wasLetter = char.IsLetter(programmerText[i]);
                wasUpperCase = char.IsUpper(programmerText[i]);
            }

            return result;
        }

        /// <summary>
        /// See if a string matches the text "iOS" on a specific index.
        /// Is used in StringUtility.GetHumanReadableText above.
        /// <see cref="GetHumanReadableText"/>
        /// </summary>
        /// <param name="programmerText">Text to match</param>
        /// <param name="i">Index to match at</param>
        /// <returns>If programmerText matches "iOS" at index i</returns>
        private static bool MatchesIOS(string programmerText, int i)
        {
            if (programmerText.Length < i + 3)
                return false;

            return programmerText[i] == 'i'
                   && programmerText[i + 1] == 'O'
                   && programmerText[i + 2] == 'S';
        }
    }
}
