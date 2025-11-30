using NUnit.Framework;
using System;
using System.Collections.Generic;
using TelegramPlugin.Extensions;

// Подключи namespace своего хелпера

namespace TelegramPlugin.Tests
{
    [TestFixture]
    public class MarkdownHelperTests
    {
        [Test]
        public void Visual_Verification()
        {
            var testCases = new Dictionary<string, string>
            {
                // --- Группа 1: Должно остаться как есть (Валидный Markdown) ---
                { "Simple Bold", "*bold text*" },
                { "Simple Italic", "_italic text_" },
                { "Simple Code", "`code block`" },
                { "Simple Link", "[Google](google.com)" },
                { "Multiple Bolds", "*bold1* and *bold2*" },
                { "Snake Case (Pairs)", "var_name_style" }, // V1 считает это курсивом "name". Это валидно.

                // --- Группа 2: Должно экранироваться (Сломанный Markdown) ---
                { "Unclosed Bold", "*bold text" },
                { "Unclosed Italic", "_italic text" },
                { "Unclosed Code", "`code block" },
                { "Math (Single *)", "2 * 2 = 4" },
                { "Filename (Single _)", "file_name.txt" },
                { "Broken Link", "[Google](google.com" }, // Нет закрывающей )
                { "Orphan Bracket", "[ text" },

                // --- Группа 3: Смешанный контекст ---
                { "Bold with orphan _", "*bold _text*" }, // _ внутри болда не имеет пары -> экранируется
                { "Code protects symbols", "`var x = *y;`" }, // * внутри кода не ломается
                { "Code protects _", "`file_name.txt`" },
                { "Escaped remains", "\\*not bold\\*" }, // Уже экранировано -> не трогаем

                // --- Группа 4: Сложные случаи ---
                {
                    "Math expression", "a * b + c * d"
                }, // Внимание: Это станет "* b + c *" (жирным). Это валидно для MD.
                { "Mixed Chaos", "*bold* `code` _italic_" },
                { "Hardcore Broken", "* _ ` [" },
                { "Double \\n", "\\n Double text *bold" },
                { "Single \n", "\n Single text *bold" }
            };

            Console.WriteLine("{0,-25} | {1,-30} | {2,-30}", "Category", "Input", "Output");
            Console.WriteLine(new string('-', 90));

            foreach (var kvp in testCases)
            {
                string input = kvp.Value;
                string output = input.SmartEscape();

                // Для визуализации заменим пробелы на точки, если строка пустая (для наглядности)
                string displayIn = string.IsNullOrEmpty(input) ? "(empty)" : input;

                Console.WriteLine("{0,-25} | {1,-30} | {2,-30}", kvp.Key, displayIn, output);
            }
        }

        // --- Автоматические проверки (Asserts) ---

        [TestCase("*bold*", "*bold*", Description = "Valid bold")]
        [TestCase("file_name.txt", "file\\_name.txt", Description = "Fix single underscore")]
        [TestCase("2 * 2", "2 \\* 2", Description = "Fix single asterisk")]
        [TestCase("`code`", "`code`", Description = "Valid code")]
        [TestCase("`code", "\\`code", Description = "Fix unclosed code")]
        [TestCase("`val_name`", "`val_name`", Description = "Underscore in code preserved")]
        [TestCase("*b _ i*", "*b \\_ i*", Description = "Fix orphan inside pair")]
        [TestCase("[Link](url)", "[Link](url)", Description = "Valid link")]
        [TestCase("[Link", "\\[Link", Description = "Fix open bracket")]
        [TestCase("\\*text", "\\*text", Description = "Keep escaped char")]
        [TestCase("*bold* _italic_", "*bold* _italic_", Description = "Multiple valid tags")]
        [TestCase("", "")]
        public void SmartEscape_Logic_Check(string input, string expected)
        {
            var result = input.SmartEscape();
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}