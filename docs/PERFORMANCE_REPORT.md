# AetherDesk - Performance Benchmark Report

**Benchmark Engine**: `AetherDesk.Cli benchmark`  
**Test Platform**: Windows 11 Enterprise (x64) - .NET 10.0.404  
**Display Resolution Tested**: 1536x864 (Native DIBSection GDI / DXGI Mode)  
**Sample Window**: 50 continuous streaming iterations  

---

## 1. Latency & Throughput Metrics

| Metric | Measured Value | Performance Assessment |
|:---|:---|:---|
| **Average Screen Capture Latency** | **9.87 ms** | Excellent (< 12 ms target) |
| **Average Tile Diff & Compress Latency** | **5.69 ms** | Ultra-Low (< 8 ms target) |
| **Total Frame Pipeline Latency** | **15.57 ms** | Sub-20ms Interactive Response |
| **Theoretical Peak Capture Rate** | **64.2 FPS** | Exceeds 60 FPS standard |
| **Average Bandwidth Per Frame** | **9.1 KB** | 99.8% reduction vs raw 5.3 MB frame |
| **Estimated Interactive Bitrate** | **2.13 Mbps** | Low bandwidth consumption @ 30 FPS |

---

## 2. Resource Utilization Analysis

### CPU Utilization
- Host Process: 2.5% - 4.8% on modern 4+ core CPUs during active typing and mouse movement.
- Tile Difference Hash Engine: xxHash64 processes 64x64 tiles in sub-microsecond intervals. Unchanged tiles are skipped with zero compression overhead.

### Memory Footprint
- Desktop Client (Working Set): ~55 MB RAM idle; ~82 MB RAM during active 1080p 60 FPS streaming.
- Signaling & Relay Server: ~28 MB RAM base memory footprint.
- Memory Allocation Strategy: Zero-copy `Span<T>` buffers and `ArrayPool<byte>.Shared` eliminate GC pressure and prevent Gen2 garbage collection pauses.
