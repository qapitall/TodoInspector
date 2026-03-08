# TodoInspector

A lightweight **Unity Editor** tool that automatically scans your C# scripts for `TODO` comments and displays them in a clean, filterable inspector window.

![Unity](https://img.shields.io/badge/Unity-6000.3%2B-black?logo=unity)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-Editor%20Only-blue)

---

## Features

- 🔍 **Automatic scanning** — scans all `.cs` files in your Assets folder on startup and keeps results up-to-date as you save or import scripts
- ⚡ **Incremental updates** — only re-scans files that actually changed (via `AssetPostprocessor`), no full re-scan needed on every save
- 🏷️ **Priority levels** — tag TODOs with `Low`, `Medium`, `High`, or `Highest` priority; each level has a distinct color indicator
- 👤 **User attribution** — assign a TODO to a specific team member using the `@username` syntax
- 🔎 **Search & filter** — filter by keyword (message, file path, or username) and/or by priority level
- 📂 **Double-click to open** — double-click any entry to jump directly to the line in your script editor
- 🎨 **Custom IMGUI UI** — a polished dark-themed editor window with virtualized list rendering for large projects

---

## Installation

### Option A — Unity Package Manager (UPM) via Git URL
1. Open **Window → Package Manager**
2. Click the **+** button → *Add package from git URL…*
3. Enter:
   ```
   https://github.com/qapitall/TodoInspector.git?path=Assets/TodoInspector
   ```

### Option B — Manual
1. Download or clone this repository
2. Copy the `Assets/TodoInspector` folder into your own project's `Assets` directory
3. Unity will compile the editor assembly automatically

---

## Usage

Open the window from the Unity menu bar:

```
Tools → TODO Inspector
```

The window will appear and immediately begin a full background scan of your project.

### TODO Syntax

Write TODO comments in any of the following formats inside your C# scripts:

```csharp
// TODO simple message here

// TODO-high Fix the null reference in this method

// TODO-medium Refactor this class when time allows

// TODO-john Add unit tests for edge cases

// TODO-john-high Rewrite the pathfinding algorithm
```

**Pattern:** `// TODO[-user][-priority] <message>`

| Segment    | Optional | Description                                           |
|------------|----------|-------------------------------------------------------|
| `user`     | ✅        | Username of the person responsible (e.g. `john`)     |
| `priority` | ✅        | One of `low`, `medium`, `high`, `highest`             |
| `message`  | ❌        | The TODO description (required)                       |

> The keyword is case-insensitive: `todo`, `TODO`, `Todo`, and `to-do` all work.

---

## Window Overview

| UI Element       | Description                                              |
|------------------|----------------------------------------------------------|
| **Search bar**   | Filters entries by message text, file path, or username  |
| **Priority dropdown** | Filters entries by a specific priority level        |
| **↻ button**     | Forces a full re-scan of all C# files                    |
| **List items**   | Show the message, file path, line number, user, and priority badge |
| **Status bar**   | Displays total and filtered TODO counts                  |

### Priority Colors

| Priority   | Color          |
|------------|----------------|
| `HIGHEST`  | 🔴 Deep Red    |
| `HIGH`     | 🟠 Red-Orange  |
| `MEDIUM`   | 🟡 Yellow      |
| `LOW`      | 🟢 Green       |
| *(none)*   | ⚫ Gray        |

---

## Project Structure

```
Assets/
└── TodoInspector/
    ├── Editor/
    │   ├── TodoEntry.cs               # Data struct for a single TODO entry
    │   ├── TodoEntryViewModel.cs      # View-layer wrapper with cached UI widths
    │   ├── TodoPriority.cs            # Priority enum (None, Low, Medium, High, Highest)
    │   ├── TodoParser.cs              # Regex-based parser for TODO comment syntax
    │   ├── TodoScanner.cs             # Async multi-threaded file scanner with cache
    │   ├── TodoScannerManager.cs      # [InitializeOnLoad] singleton, event bus
    │   ├── TodoAssetPostprocessor.cs  # Hooks into Unity's import pipeline for incremental updates
    │   └── TodoWindow.cs              # EditorWindow — IMGUI rendering & interaction
    └── Tests/
        └── Editor/
            ├── TodoParserPerformanceTests.cs
            ├── TodoScannerCacheTests.cs
            └── TodoScannerPerformanceTests.cs
```

---

## Tests

The project includes **43 unit and performance tests** covering parsing, caching, and scanning behavior.

| Category       | Description                                                   |
|----------------|---------------------------------------------------------------|
| **Correctness**| Validates TODO parsing, priority extraction, user attribution |
| **Cache**      | Ensures per-file cache independence, add/remove/update cycles |
| **Performance**| Benchmarks parser throughput, GC allocation, scan latency     |
| **Async**      | Verifies background full-scan, cancellation, and concurrency  |

**Latest Run:**

| Metric         | Value          |
|----------------|----------------|
| Total Tests    | 43             |
| Passed         | 43             |
| Failed         | 0              |
| Skipped        | 0              |
| Duration       | ~0.04 s        |
| Engine         | NUnit 3.5.0    |
| Platform       | Unity EditMode |

![Tests](https://img.shields.io/badge/tests-43%20passed-brightgreen)

### Performance Benchmarks

| Operation / Scenario | Condition / Load | Execution Time | GC Allocation | Threshold Goal |
| :--- | :--- | :--- | :--- | :--- |
| **Parser: Early Exit** | 10,000 lines (No TODOs) | **27 ms** | 0 bytes | < 100 ms |
| **Parser: Full Parse** | 10,000 lines (Mixed content) | **110 ms** | - | < 200 ms |
| **Scanner: Large File** | 1,000 lines (100 TODOs) | **2 ms** | - | < 100 ms |
| **Scanner: Sequential** | 100 files (10 TODOs each) | **58 ms** | - | < 500 ms |
| **Cache: Fetch All** | 1,000 cached entries | **0 ms** | 48 KB | < 10 ms |
| **Memory: Missed Regex** | 1,000 iterations | - | **0 bytes** | < 50 KB |
| **Memory: Matched Regex** | 1,000 iterations | - | **32 KB** | < 200 KB |

---

## Requirements

- **Unity 2021.3 or later** (developed and tested on Unity 6000.3)
- **.NET 4.x** scripting runtime (default in modern Unity)

---

## License

This project is licensed under the [MIT License](LICENSE).
