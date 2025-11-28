using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramPlugin.Extensions;

public static class MarkdownHelper
{
    extension(string text)
    {
        private string NormalizeLineBreaks() =>
            text.Replace("\\n", "\n").Replace("\\r", "\r");

        public string SmartEscape()
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = text.NormalizeLineBreaks();

            var sb = new StringBuilder(text.Length + 10);

            var state = new Dictionary<char, bool>
            {
                { '*', false },
                { '_', false },
                { '`', false }
            };

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '\\' && i + 1 < text.Length)
                {
                    sb.Append(c).Append(text[++i]);
                    continue;
                }

                // логика для блока кода `
                if (state['`'] && c != '`')
                {
                    sb.Append(c);
                    continue;
                }

                // Обработка парных тегов
                if (state.ContainsKey(c))
                {
                    HandleTag(c, text, i, sb, state);
                    continue;
                }

                // Ссылки
                if (c == '[')
                {
                    if (text.IndexOf("](", i, StringComparison.Ordinal) > -1) sb.Append(c);
                    else sb.Append("\\[");
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }
    }

    private static void HandleTag(char tag, string text, int index, StringBuilder sb, Dictionary<char, bool> state)
    {
        var isOpen = state[tag];

        if (!isOpen)
        {
            if (HasClosingPair(text, index + 1, tag))
            {
                state[tag] = true;
                sb.Append(tag);
            }
            else
                sb.Append('\\').Append(tag);
        }
        else
        {
            state[tag] = false;
            sb.Append(tag);
        }
    }

    private static bool HasClosingPair(string text, int startIndex, char target)
    {
        for (var k = startIndex; k < text.Length; k++)
        {
            if (text[k] == '\\')
            {
                k++;
                continue;
            }

            if (text[k] == target) return true;
        }

        return false;
    }
}