---
name: unity-performance
description: Investigate measured Unity performance problems in this project. Use for frame drops, stutter, GC pressure, excessive Update work, physics/UI cadence, pooling decisions, or networking and voice hotspots.
---

# Unity performance

Read [AGENTS.md](../../../AGENTS.md). Use `$unity-diagnosing-bugs` when the request is diagnosis-only.

1. Define one observable metric and capture a baseline in the smallest representative scenario. Prefer Unity Profiler evidence over code inspection.
2. Attribute the hotspot before changing code. Inspect likely causes only in the measured path:
   - unrelated `Update`, `LateUpdate`, or `FixedUpdate` loops;
   - repeated `Find`, `GetComponent`, `Camera.main`, or tag lookups;
   - per-frame LINQ, formatting, closures, boxing, or logs;
   - repeated `Instantiate`/`Destroy` where pooling has measured value;
   - physics, animation, UI, voice, or network work at the wrong cadence;
   - reflection or Editor-only helpers in runtime paths.
3. Rank candidate changes by expected frame-time, GC, or scalability gain and implementation risk. Reject speculative micro-optimization.
4. Apply one authorized change at a time and repeat the identical measurement.
5. Report baseline, result, noise/limitations, and changes intentionally deferred. Use `$unity-change-verification` for affected code.

Preserve clarity unless evidence supports the tradeoff. Complete when the before/after comparison uses the same scenario and the claimed gain exceeds measurement noise.
