using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RenderDocPlugins
{
    public class CSV
    {
        public const char k_Spliter = ',';
        public const char k_Quota = '"';

        public static string GetCSVString(string line, int index)
        {
            int beginCharIndex = -1;
            int endCharIndex = -1;
            int curIndex = 0;
            bool beginQuota = false;
            int charIndex = 0;
            while (charIndex < line.Length)
            {
                var ch = line[charIndex];
                //if(!char.IsWhiteSpace(ch))
                //{
                bool isNormalChar = false;
                if (ch == k_Quota)
                {
                    if (beginQuota)
                    {
                        beginQuota = false;
                        if (curIndex == index)
                        {
                            endCharIndex = charIndex - 1;
                            break;
                        }
                    }
                    else
                    {
                        beginQuota = true;
                    }
                }
                else if (ch == k_Spliter)
                {
                    if (!beginQuota)
                    {
                        // next
                        if (curIndex == index)
                        {
                            endCharIndex = charIndex - 1;
                            break;
                        }
                        ++curIndex;
                    }
                    else
                    {
                        isNormalChar = true;
                    }
                }
                else
                {
                    isNormalChar = true;
                }

                if (isNormalChar)
                {
                    if (curIndex == index)
                    {
                        if (beginCharIndex == -1)
                        {
                            beginCharIndex = charIndex;
                        }
                    }
                }
                //}
                ++charIndex;
            }
            if ((beginCharIndex != -1) && (endCharIndex == -1))
            {
                endCharIndex = line.Length - 1;
            }
            if ((beginCharIndex == -1) && (endCharIndex != -1))
            {
                beginCharIndex = endCharIndex;
            }
            if ((beginCharIndex != -1) && (endCharIndex != -1) && (endCharIndex >= beginCharIndex))
            {
                return line.Substring(beginCharIndex, endCharIndex - beginCharIndex + 1);
            }
            return string.Empty;
        }
    }

}