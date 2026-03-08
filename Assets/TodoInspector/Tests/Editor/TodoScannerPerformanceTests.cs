using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace TodoInspector.Tests
{
    [TestFixture]
    internal class TodoScannerPerformanceTests
    {
        private string _tempDir;
        private TodoScanner _scanner;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "TodoInspectorTests_" + Guid.NewGuid().ToString("N"));
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
        [Category("Performance")]
        public void ScanFileImmediate_LargeFile_1000Lines100Todos_CompletesUnder100ms()
        {
            string filePath = CreateTempFile("LargeFile.cs", lines: 1_000, todosEvery: 10);

            var sw = Stopwatch.StartNew();
            _scanner.ScanFileImmediate(filePath);
            sw.Stop();

            int count = _scanner.GetEntryCount();

            Assert.AreEqual(100, count);
            Assert.Less(sw.ElapsedMilliseconds, 100);
        }

        [Test]
        [Category("Performance")]
        public void ScanFileImmediate_SmallFile_NoTodos_CompletesUnder20ms()
        {
            string filePath = CreateTempFile("SmallFile.cs", lines: 50, todosEvery: 0);

            var sw = Stopwatch.StartNew();
            _scanner.ScanFileImmediate(filePath);
            sw.Stop();

            Assert.AreEqual(0, _scanner.GetEntryCount());
            Assert.Less(sw.ElapsedMilliseconds, 20);
        }

        [Test]
        [Category("Performance")]
        public void ScanFileImmediate_100Files_10TodosEach_TotalUnder500ms()
        {
            var files = new List<string>(100);
            for (int i = 0; i < 100; i++)
                files.Add(CreateTempFile($"File_{i:D3}.cs", lines: 50, todosEvery: 5));

            var sw = Stopwatch.StartNew();
            foreach (string f in files)
                _scanner.ScanFileImmediate(f);
            sw.Stop();

            int count = _scanner.GetEntryCount();

            Assert.AreEqual(100 * 10, count);
            Assert.Less(sw.ElapsedMilliseconds, 500);
        }

        [Test]
        [Category("Performance")]
        public void GetAllEntries_1000Entries_CompletesUnder10ms()
        {
            for (int i = 0; i < 100; i++)
            {
                string f = CreateTempFile($"Entry_{i:D3}.cs", lines: 30, todosEvery: 3);
                _scanner.ScanFileImmediate(f);
            }

            Assert.AreEqual(100 * 10, _scanner.GetEntryCount());

            var sw = Stopwatch.StartNew();
            List<TodoEntry> entries = _scanner.GetAllEntries();
            sw.Stop();

            Assert.AreEqual(1000, entries.Count);
            Assert.Less(sw.ElapsedMilliseconds, 10);
        }

        [Test]
        [Category("Performance")]
        public void GetEntryCount_1000Entries_CompletesUnder5ms()
        {
            for (int i = 0; i < 100; i++)
            {
                string f = CreateTempFile($"Count_{i:D3}.cs", lines: 30, todosEvery: 3);
                _scanner.ScanFileImmediate(f);
            }

            var sw = Stopwatch.StartNew();
            int count = _scanner.GetEntryCount();
            sw.Stop();

            Assert.AreEqual(1000, count);
            Assert.Less(sw.ElapsedMilliseconds, 5);
        }

        [Test]
        [Category("Performance")]
        public void GetAllEntries_GcAllocation_AcceptableUnder200KB()
        {
            for (int i = 0; i < 100; i++)
            {
                string f = CreateTempFile($"Gc_{i:D3}.cs", lines: 30, todosEvery: 3);
                _scanner.ScanFileImmediate(f);
            }

            _scanner.GetAllEntries();

            long before = GC.GetTotalMemory(forceFullCollection: false);
            _scanner.GetAllEntries();
            long after = GC.GetTotalMemory(forceFullCollection: false);

            long delta = after - before;
            Assert.Less(delta, 200 * 1024);
        }

        [Test]
        [Category("Cache")]
        public void ScanFile_ThenRemove_EntryCountIsZero()
        {
            string filePath = CreateTempFile("Removable.cs", lines: 20, todosEvery: 5);
            _scanner.ScanFileImmediate(filePath);
            Assert.AreEqual(4, _scanner.GetEntryCount());

            _scanner.RemoveFile(filePath);
            Assert.AreEqual(0, _scanner.GetEntryCount());
        }

        [Test]
        [Category("Cache")]
        public void ScanFile_Modified_UpdatesCache()
        {
            string filePath = CreateTempFile("Modifiable.cs", lines: 20, todosEvery: 10);
            _scanner.ScanFileImmediate(filePath);
            Assert.AreEqual(2, _scanner.GetEntryCount());

            string newContent = BuildFileContent(lines: 50, todosEvery: 10);
            File.WriteAllText(filePath, newContent);

            _scanner.ScanFileImmediate(filePath);
            Assert.AreEqual(5, _scanner.GetEntryCount());
        }

        [Test]
        [Category("Cache")]
        public void ScanFile_MultipleFiles_IndependentCache()
        {
            string file1 = CreateTempFile("IndepA.cs", lines: 20, todosEvery: 5);
            string file2 = CreateTempFile("IndepB.cs", lines: 30, todosEvery: 10);
            string file3 = CreateTempFile("IndepC.cs", lines: 40, todosEvery: 20);

            _scanner.ScanFileImmediate(file1);
            _scanner.ScanFileImmediate(file2);
            _scanner.ScanFileImmediate(file3);
            Assert.AreEqual(9, _scanner.GetEntryCount());

            File.WriteAllText(file2, BuildFileContent(lines: 10, todosEvery: 5));
            _scanner.ScanFileImmediate(file2);

            Assert.AreEqual(8, _scanner.GetEntryCount());
        }

        [Test]
        [Category("Cache")]
        public void ScanFileImmediate_FileNotExists_RemovesFromCache()
        {
            string filePath = CreateTempFile("WillBeDeleted.cs", lines: 10, todosEvery: 5);
            _scanner.ScanFileImmediate(filePath);
            Assert.AreEqual(2, _scanner.GetEntryCount());

            File.Delete(filePath);
            _scanner.ScanFileImmediate(filePath);
            Assert.AreEqual(0, _scanner.GetEntryCount());
        }

        [Test]
        [Category("Cache")]
        public void ScanFileImmediate_SameFileTwice_ConsistentResult()
        {
            string filePath = CreateTempFile("Consistent.cs", lines: 20, todosEvery: 5);

            _scanner.ScanFileImmediate(filePath);
            int firstCount = _scanner.GetEntryCount();

            _scanner.ScanFileImmediate(filePath);
            int secondCount = _scanner.GetEntryCount();

            Assert.AreEqual(firstCount, secondCount);
        }

        [Test]
        [Category("Async")]
        public void StartFullScan_EmptyDirectory_CompletesWithin5Seconds()
        {
            string emptyDir = Path.Combine(_tempDir, "Empty");
            Directory.CreateDirectory(emptyDir);

            var scannerEmpty = new TodoScanner(emptyDir, excludePath: string.Empty);
            var completed = new ManualResetEventSlim(false);
            scannerEmpty.OnScanCompleted += () => completed.Set();

            scannerEmpty.StartFullScan();

            bool finished = completed.Wait(TimeSpan.FromSeconds(5));
            Assert.IsTrue(finished);
            Assert.AreEqual(0, scannerEmpty.GetEntryCount());
        }

        [Test]
        [Category("Async")]
        public void StartFullScan_WithFiles_FindsAllTodos()
        {
            for (int i = 0; i < 10; i++)
                CreateTempFile($"AsyncScan_{i:D2}.cs", lines: 30, todosEvery: 6);

            var sw = Stopwatch.StartNew();
            var completed = new ManualResetEventSlim(false);
            _scanner.OnScanCompleted += () => { sw.Stop(); completed.Set(); };

            _scanner.StartFullScan();

            bool finished = completed.Wait(TimeSpan.FromSeconds(10));
            int count = _scanner.GetEntryCount();
            Assert.IsTrue(finished);
            Assert.AreEqual(10 * 5, count);
        }

        [Test]
        [Category("Async")]
        public void CancelScan_DuringOperation_DoesNotThrow()
        {
            for (int i = 0; i < 50; i++)
                CreateTempFile($"Cancel_{i:D2}.cs", lines: 100, todosEvery: 10);

            Assert.DoesNotThrow(() =>
            {
                _scanner.StartFullScan();
                _scanner.CancelScan();
            });
        }

        [Test]
        [Category("Async")]
        public void StartFullScan_MultipleConsecutive_OnlyLastCompletes()
        {
            for (int i = 0; i < 10; i++)
                CreateTempFile($"Consec_{i:D2}.cs", lines: 20, todosEvery: 5);

            int completionCount = 0;
            var lastCompleted = new ManualResetEventSlim(false);

            _scanner.OnScanCompleted += () =>
            {
                Interlocked.Increment(ref completionCount);
                lastCompleted.Set();
            };

            _scanner.StartFullScan();
            _scanner.StartFullScan();
            _scanner.StartFullScan();

            bool finished = lastCompleted.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(finished);
        }

        private string CreateTempFile(string fileName, int lines, int todosEvery)
        {
            string path = Path.Combine(_tempDir, fileName);
            File.WriteAllText(path, BuildFileContent(lines, todosEvery));
            return path;
        }

        private static string BuildFileContent(int lines, int todosEvery)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("// Auto-generated test file");
            int todoIndex = 0;
            for (int i = 1; i <= lines; i++)
            {
                if (todosEvery > 0 && i % todosEvery == 0)
                {
                    todoIndex++;
                    sb.AppendLine($"    // TODO fix item #{todoIndex} on line {i}");
                }
                else
                {
                    sb.AppendLine($"    int x{i} = {i};");
                }
            }
            return sb.ToString();
        }
    }
}















