# Advanced WPF & .NET Performance Diagnostics: A Teacher's Guide

Welcome! As a senior (.NET/WPF) developer, solving performance issues requires moving past "trial-and-error" and using a structured, scientific approach. When an application slows down after 30 minutes in production, it is almost always a slow-growing leak or queue accumulation.

Below is your diagnostic playbook to identify, analyze, and fix these issues.

---

## 1. The Professional Diagnostics Toolchain

Before trying to change code, you must know how to inspect the running process. Here are the tools of the trade:

| Tool | Best Used For | What it Tells You |
| :--- | :--- | :--- |
| **dotnet-gcdump** | Lightweight Memory Snapshots | Fast heap snapshots in production without installing heavy software. |
| **Visual Studio Diagnostic Tools** | Real-time debugging | Live CPU, memory charts, and GC events while running locally. |
| **JetBrains dotMemory / dotTrace** | Deep memory & profiling analysis | Identifies retention paths (who is holding onto leaked objects) and hot path call stacks. |
| **PerfView** | Hardcore CPU/GC tracing | Detailed GC pause durations, LOH allocation allocations, and JIT compilation overhead. |

---

## 2. Step-by-Step Diagnostic Workflow

When a performance issue is reported, follow these exact steps:

### Step 1: Establish a Baseline (Start State)
1. Start the application and let it settle for 1 minute.
2. Capture a memory snapshot (e.g., using `dotnet-gcdump collect -p <PID>`).
3. Note the heap size and object count of key classes (e.g., `Window`, `ViewModels`, `Byte[]`).

### Step 2: Perform the Exercise (Stress Phase)
1. Perform the suspect user action repeatedly (e.g., opening and closing a customer detail view 50 times).
2. Take a second snapshot immediately after completing the exercise.
3. Force a Garbage Collection (GC) to run (in our sandbox, click the **Clear Leaks / Reset GC** button; in Visual Studio, click the **Force GC** button).
4. Take a third snapshot after GC.

### Step 3: Analyze Retained Objects (The "Why")
Compare Snapshot 1 and Snapshot 3:
- **Expected result**: The count of window and viewmodel objects should return to the baseline count.
- **Leaked result**: The count remains high. 
- **How to read the retention path**: In your profiling tool (like dotMemory or Visual Studio's Memory Usage analyzer), look at the **Key Retention Path** for the leaked object. It will show a chain of references leading to a static root (e.g., a static event handler, a static list, or a running thread).

---

## 3. Advanced Diagnostic Concepts

As an experienced developer, watch out for these subtle traps:

### Concept A: LOH Fragmentation & Object Pinning
- **The Problem**: Objects $> 85,000$ bytes (large string buffers, big byte arrays, large bitmaps) go directly to the **Large Object Heap (LOH)**. LOH is not compacted by default in older .NET versions. If you pin buffers (using `fixed` statements or `GCHandleType.Pinned`), GC cannot move them. Over time, memory becomes "holy" (fragmented), and allocating a new large object fails with an `OutOfMemoryException` even though there is plenty of total free space.
- **How to Identify**: In PerfView or dotMemory, check the LOH size and fragmentation percentage.
- **The Fix**: Use the `ArrayPool<T>` class to reuse large buffers instead of allocating new ones, or set `GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;` before forcing a GC collection.

### Concept B: Layout Cycles (Visual & Logical Tree Overhead)
- **The Problem**: WPF uses a two-pass layout system: `Measure` (how big do you want to be?) and `Arrange` (here is your space). If you write custom controls or bad bindings that trigger layout recalculations continuously during rendering, you create a layout cycle loop.
- **How to Identify**: In Visual Studio's Live Visual Tree analyzer, watch the CPU load when hovering over or resizing elements. If CPU stays high during idle times, a layout loop is occurring.
- **The Fix**: Minimize visual tree nesting, avoid using `Auto` sizes inside deeply nested layouts when possible, and ensure custom controls call `InvalidateMeasure()` only when sizing inputs change.

### Concept C: Thread Pool Starvation
- **The Problem**: Blocking asynchronous calls using `.Result` or `.Wait()` locks up ThreadPool threads. If the UI thread blocks waiting for a background thread, and the background thread needs the UI thread to dispatch work, you get a deadlock. Under heavy load, ThreadPool threads exhaust, and the dispatcher queue blocks.
- **How to Identify**: Monitor the thread count. If it climbs continuously and the UI freezes, starvation is likely.
- **The Fix**: Use proper `async` / `await` all the way up the call stack. Never block using `.Result` or `.Wait()`.
