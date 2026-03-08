using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace TodoInspector.Tests
{
    [TestFixture]
    [Category("Cache")]
    internal class TodoScannerCacheTests
    {
        private string _tempDir;
        private TodoScanner _scanner;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "TodoCache_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _scanner = new TodoScanner(_tempDir, excludePath: string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); }
            catch { }
        }

        [Test]
        public void InitialState_EntryCountIsZero(){
            Assert.AreEqual(0, _scanner.GetAllEntries().Count);
        }

        [Test]
        public void ScanEmptyFile_EntryCountRemainsZero()
        {
            string path = WriteFile("Empty.cs", "// no todos here\nint x = 1;\n");
            _scanner.ScanFileImmediate(path);
            Assert.AreEqual(0, _scanner.GetEntryCount());
        }

        [Test]
        public void ScanFile_AddsEntriesToCache()
        {
            string path = WriteFile("HasTodos.cs",
                "// TODO fix this\n// TODO-high critical\n// TODO-user-medium review\n");

            _scanner.ScanFileImmediate(path);
            Assert.AreEqual(3, _scanner.GetEntryCount());
        }

        [Test]
        public void RemoveFile_ClearsEntriesForThatFile()
        {
            string path = WriteFile("Removable.cs", "// TODO fix\n// TODO cleanup\n");
            _scanner.ScanFileImmediate(path);
            Assert.AreEqual(2, _scanner.GetEntryCount());

            _scanner.RemoveFile(path);
            Assert.AreEqual(0, _scanner.GetEntryCount());
        }

        [Test]
        public void RemoveFile_NonExistentKey_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                _scanner.RemoveFile(Path.Combine(_tempDir, "ghost.cs")));
        }

        [Test]
        public void ScanFile_OverwritesExistingCacheForSameFile()
        {
            string path = WriteFile("Overwrite.cs", "// TODO first\n");
            _scanner.ScanFileImmediate(path);
            Assert.AreEqual(1, _scanner.GetEntryCount());

            File.WriteAllText(path, "// TODO a\n// TODO b\n// TODO c\n");
            _scanner.ScanFileImmediate(path);
            Assert.AreEqual(3, _scanner.GetEntryCount());
        }

        [Test]
        public void ScanFile_ThenRemoveTodos_EntryCountDrops()
        {
            string path = WriteFile("Reduce.cs", "// TODO a\n// TODO b\n// TODO c\n");
            _scanner.ScanFileImmediate(path);
            Assert.AreEqual(3, _scanner.GetEntryCount());

            File.WriteAllText(path, "int x = 1;\nint y = 2;\n");
            _scanner.ScanFileImmediate(path);
            Assert.AreEqual(0, _scanner.GetEntryCount());
        }

        [Test]
        public void MultipleFiles_EachHasIndependentCache()
        {
            string a = WriteFile("A.cs", "// TODO in A\n");
            string b = WriteFile("B.cs", "// TODO in B\n// TODO also B\n");
            string c = WriteFile("C.cs", "// TODO in C\n// TODO c2\n// TODO c3\n");

            _scanner.ScanFileImmediate(a);
            _scanner.ScanFileImmediate(b);
            _scanner.ScanFileImmediate(c);

            Assert.AreEqual(6, _scanner.GetEntryCount());

            _scanner.RemoveFile(b);
            Assert.AreEqual(4, _scanner.GetEntryCount());

            List<TodoEntry> all = _scanner.GetAllEntries();
            foreach (var entry in all)
                Assert.IsFalse(entry.FilePath.Contains("B.cs"));
        }

        [Test]
        public void UpdateOneFile_OthersUnchanged()
        {
            string a = WriteFile("StableA.cs", "// TODO a1\n// TODO a2\n");
            string b = WriteFile("StableB.cs", "// TODO b1\n");
            string c = WriteFile("StableC.cs", "// TODO c1\n// TODO c2\n// TODO c3\n");

            _scanner.ScanFileImmediate(a);
            _scanner.ScanFileImmediate(b);
            _scanner.ScanFileImmediate(c);
            Assert.AreEqual(6, _scanner.GetEntryCount());

            File.WriteAllText(b, "// TODO b1\n// TODO b2\n// TODO b3\n");
            _scanner.ScanFileImmediate(b);

            Assert.AreEqual(8, _scanner.GetEntryCount());
        }

        [Test]
        public void GetAllEntries_ContainsCorrectFilePaths()
        {
            string a = WriteFile("PathA.cs", "// TODO from A\n");
            string b = WriteFile("PathB.cs", "// TODO from B\n");

            _scanner.ScanFileImmediate(a);
            _scanner.ScanFileImmediate(b);

            List<TodoEntry> entries = _scanner.GetAllEntries();
            bool hasA = false, hasB = false;

            foreach (var e in entries)
            {
                if (e.FilePath.Contains("PathA.cs")) hasA = true;
                if (e.FilePath.Contains("PathB.cs")) hasB = true;
            }

            Assert.IsTrue(hasA, "PathA.cs entry missing");
            Assert.IsTrue(hasB, "PathB.cs entry missing");
        }

        [Test]
        public void GetAllEntries_LineNumbersAreCorrect()
        {
            string content = "int x = 1;\n// TODO fix line 2\nint y = 3;\n// TODO line 4\n";
            string path = WriteFile("LineNums.cs", content);
            _scanner.ScanFileImmediate(path);

            List<TodoEntry> entries = _scanner.GetAllEntries();
            Assert.AreEqual(2, entries.Count);

            bool hasLine2 = false, hasLine4 = false;
            foreach (var e in entries)
            {
                if (e.LineNumber == 2) hasLine2 = true;
                if (e.LineNumber == 4) hasLine4 = true;
            }

            Assert.IsTrue(hasLine2, "TODO on line 2 not found");
            Assert.IsTrue(hasLine4, "TODO on line 4 not found");
        }

        [Test]
        public void GetAllEntries_PriorityPreserved()
        {
            string path = WriteFile("Priority.cs",
                "// TODO-low low item\n// TODO-high high item\n// TODO-highest critical\n");
            _scanner.ScanFileImmediate(path);

            List<TodoEntry> entries = _scanner.GetAllEntries();
            Assert.AreEqual(3, entries.Count);

            bool hasLow = false, hasHigh = false, hasHighest = false;
            foreach (var e in entries)
            {
                if (e.Priority == TodoPriority.Low) hasLow = true;
                if (e.Priority == TodoPriority.High) hasHigh = true;
                if (e.Priority == TodoPriority.Highest) hasHighest = true;
            }

            Assert.IsTrue(hasLow, "Low priority entry missing");
            Assert.IsTrue(hasHigh, "High priority entry missing");
            Assert.IsTrue(hasHighest, "Highest priority entry missing");
        }

        [Test]
        public void GetAllEntries_UserPreserved()
        {
            string path = WriteFile("Users.cs",
                "// TODO-alice fix this\n// TODO-bob-high review\n");
            _scanner.ScanFileImmediate(path);

            List<TodoEntry> entries = _scanner.GetAllEntries();
            bool hasAlice = false, hasBob = false;

            foreach (var e in entries)
            {
                if (e.User == "alice") hasAlice = true;
                if (e.User == "bob") hasBob = true;
            }

            Assert.IsTrue(hasAlice, "alice user entry missing");
            Assert.IsTrue(hasBob, "bob user entry missing");
        }

        [Test]
        public void ScanFile_EmptyFileContent_NoEntries()
        {
            string path = WriteFile("Empty2.cs", string.Empty);
            _scanner.ScanFileImmediate(path);
            Assert.AreEqual(0, _scanner.GetEntryCount());
        }

        [Test]
        public void ScanFile_FileWithOnlyWhitespace_NoEntries()
        {
            string path = WriteFile("Whitespace.cs", "   \n\t\n   \n");
            _scanner.ScanFileImmediate(path);
            Assert.AreEqual(0, _scanner.GetEntryCount());
        }

        [Test]
        public void ScanFile_TodoInStringLiteral_NotParsed()
        {
            string path = WriteFile("StringLiteral.cs",
                "string msg = \"TODO fix this\";\nvar s = $\"TODO {x}\";\n");
            _scanner.ScanFileImmediate(path);
            Assert.AreEqual(0, _scanner.GetEntryCount());
        }

        [Test]
        [Category("Performance")]
        public void GetAllEntries_CalledRepeatedly_PerformanceStable()
        {
            for (int i = 0; i < 50; i++)
                WriteFileAndScan($"Repeat_{i:D2}.cs", lines: 20, todosEvery: 5);

            _scanner.GetAllEntries();

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                _scanner.GetAllEntries();
            sw.Stop();

            Assert.Less(sw.ElapsedMilliseconds, 100);
        }

        private string WriteFile(string name, string content)
        {
            string path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, content);
            return path;
        }

        private void WriteFileAndScan(string name, int lines, int todosEvery)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 1; i <= lines; i++)
            {
                if (todosEvery > 0 && i % todosEvery == 0)
                    sb.AppendLine($"    // TODO item {i}");
                else
                    sb.AppendLine($"    int x{i} = {i};");
            }
            string path = WriteFile(name, sb.ToString());
            _scanner.ScanFileImmediate(path);
        }
    }
}


















