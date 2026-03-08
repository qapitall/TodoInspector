using System;
using System.Diagnostics;
using NUnit.Framework;

namespace TodoInspector.Tests
{
    [TestFixture]
    [Category("Performance")]
    internal class TodoParserPerformanceTests
    {
        private const string FilePath = "Assets/FakeFile.cs";

        [Test]
        [Category("Correctness")]
        public void Parse_BasicTodo_ReturnsEntry()
        {
            bool result = TodoParser.TryParse("// TODO fix this", 1, FilePath, out TodoEntry entry);

            Assert.IsTrue(result);
            Assert.AreEqual("fix this", entry.Message);
            Assert.AreEqual(TodoPriority.None, entry.Priority);
            Assert.IsNull(entry.User);
        }

        [Test]
        [Category("Correctness")]
        public void Parse_AllPriorityVariants_CorrectlyParsed()
        {
            (string line, TodoPriority expected)[] cases =
            {
                ("// TODO-low   low priority task",     TodoPriority.Low),
                ("// TODO-medium medium priority task",  TodoPriority.Medium),
                ("// TODO-high   high priority task",    TodoPriority.High),
                ("// TODO-highest critical task",        TodoPriority.Highest),
            };

            foreach (var (line, expected) in cases)
            {
                bool ok = TodoParser.TryParse(line, 1, FilePath, out TodoEntry entry);
                Assert.IsTrue(ok, $"Parse failed: {line}");
                Assert.AreEqual(expected, entry.Priority, $"Wrong priority: {line}");
            }
        }

        [Test]
        [Category("Correctness")]
        public void Parse_UserAndPriority_BothExtracted()
        {
            bool ok = TodoParser.TryParse("// TODO-sercan-high refactor this method", 5, FilePath, out TodoEntry entry);

            Assert.IsTrue(ok);
            Assert.AreEqual("sercan", entry.User);
            Assert.AreEqual(TodoPriority.High, entry.Priority);
            Assert.AreEqual("refactor this method", entry.Message);
        }

        [Test]
        [Category("Correctness")]
        public void Parse_NullLine_ReturnsFalse()
        {
            bool result = TodoParser.TryParse(null, 1, FilePath, out _);
            Assert.IsFalse(result);
        }

        [Test]
        [Category("Correctness")]
        public void Parse_NoComment_ReturnsFalse()
        {
            bool result = TodoParser.TryParse("string x = \"TODO do something\";", 1, FilePath, out _);
            Assert.IsFalse(result);
        }

        [Test]
        [Category("Correctness")]
        public void Parse_ToDoDash_Variant_Parsed()
        {
            bool ok = TodoParser.TryParse("// TO-DO fix this too", 1, FilePath, out TodoEntry entry);
            Assert.IsTrue(ok);
            Assert.AreEqual("fix this too", entry.Message);
        }

        [Test]
        public void Parse_10000Lines_MixedContent_CompletesUnder200ms()
        {
            string[] lines = BuildMixedLines(10_000);

            var sw = Stopwatch.StartNew();
            int found = 0;
            foreach (string line in lines)
            {
                if (TodoParser.TryParse(line, 1, FilePath, out _))
                    found++;
            }
            sw.Stop();

            Assert.Less(sw.ElapsedMilliseconds, 200);
        }

        [Test]
        public void Parse_10000NoTodoLines_EarlyExitFast_CompletesUnder100ms()
        {
            string[] lines = new string[10_000];
            for (int i = 0; i < lines.Length; i++)
                lines[i] = $"    // regular comment line {i}";

            var sw = Stopwatch.StartNew();
            foreach (string line in lines)
                TodoParser.TryParse(line, 1, FilePath, out _);
            sw.Stop();

            Assert.Less(sw.ElapsedMilliseconds, 100);
        }

        [Test]
        public void Parse_10000TodoLines_AllMatch_CompletesUnder300ms()
        {
            string[] lines = new string[10_000];
            for (int i = 0; i < lines.Length; i++)
                lines[i] = $"// TODO-high fix item {i}";

            var sw = Stopwatch.StartNew();
            int found = 0;
            foreach (string line in lines)
                if (TodoParser.TryParse(line, 1, FilePath, out _)) found++;
            sw.Stop();

            Assert.AreEqual(10_000, found);
            Assert.Less(sw.ElapsedMilliseconds, 300);
        }

        [Test]
        public void Parse_GcAllocation_FailedMatch_IsLow()
        {
            for (int i = 0; i < 100; i++)
                TodoParser.TryParse("// regular comment", 1, FilePath, out _);

            long before = GC.GetTotalMemory(forceFullCollection: false);
            for (int i = 0; i < 1_000; i++)
                TodoParser.TryParse("// regular comment", 1, FilePath, out _);
            long after = GC.GetTotalMemory(forceFullCollection: false);

            long delta = after - before;
            Assert.Less(delta, 50 * 1024);
        }

        [Test]
        public void Parse_GcAllocation_SuccessfulMatch_AcceptableUnder200KB()
        {
            for (int i = 0; i < 100; i++)
                TodoParser.TryParse("// TODO-high fix item", 1, FilePath, out _);

            long before = GC.GetTotalMemory(forceFullCollection: false);
            for (int i = 0; i < 1_000; i++)
                TodoParser.TryParse($"// TODO-sercan-high fix item {i}", 1, FilePath, out _);
            long after = GC.GetTotalMemory(forceFullCollection: false);

            long delta = after - before;
            Assert.Less(delta, 200 * 1024);
        }

        private static string[] BuildMixedLines(int count)
        {
            string[] lines = new string[count];
            for (int i = 0; i < count; i++)
            {
                switch (i % 5)
                {
                    case 0: lines[i] = $"// TODO fix item {i}"; break;
                    case 1: lines[i] = $"    int x = {i};"; break;
                    case 2: lines[i] = $"// TODO-high critical issue {i}"; break;
                    case 3: lines[i] = $"    // just a comment {i}"; break;
                    case 4: lines[i] = $"// TODO-user-medium review this {i}"; break;
                }
            }
            return lines;
        }
    }
}










