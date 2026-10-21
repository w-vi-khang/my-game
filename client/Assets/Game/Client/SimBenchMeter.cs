using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Core.Sim;
using Debug = UnityEngine.Debug;

namespace Game.Client
{
    /// <summary>Bộ đo sim bước 7 (brief 0028): thời gian mỗi Tick, ghi dòng SIMBENCH mỗi 10 giây và một dòng TONG khi đủ thời gian.</summary>
    public sealed class SimBenchMeter
    {
        public const float WindowSeconds = 10f;

        readonly float totalSeconds;
        readonly List<double> window = new List<double>();
        readonly List<double> all = new List<double>();
        bool started;
        float start, windowStart;
        long astar0, bfs0, astarW0, bfsW0;
        int framesW, framesAll, maxTicksW, maxTicksAll;
        double dtW, dtAll;

        public SimBenchMeter(float totalSeconds) { this.totalSeconds = totalSeconds; }

        public bool Done { get; private set; }
        /// <summary>Dòng SIMBENCH gần nhất (cửa sổ hoặc TONG).</summary>
        public string LastLine { get; private set; }
        /// <summary>Dòng TONG; null tới khi Done.</summary>
        public string Summary { get; private set; }

        public void AddTick(long stopwatchTicks)
        {
            if (Done) return;
            double ms = stopwatchTicks * 1000.0 / Stopwatch.Frequency;
            window.Add(ms);
            all.Add(ms);
        }

        public void AddFrame(float now, float dt, int ticksThisFrame, Simulation sim)
        {
            if (Done) return;
            if (!started)
            {
                started = true;
                start = windowStart = now;
                astar0 = astarW0 = sim.AStarCount;
                bfs0 = bfsW0 = sim.BfsCount;
            }
            framesW++;
            framesAll++;
            dtW += dt;
            dtAll += dt;
            if (ticksThisFrame > maxTicksW) maxTicksW = ticksThisFrame;
            if (ticksThisFrame > maxTicksAll) maxTicksAll = ticksThisFrame;

            if (now - windowStart >= WindowSeconds)
            {
                double span = now - windowStart;
                window.Sort();
                double avg = Average(window);
                LastLine = $"SIMBENCH t={now - start:F0}s ticks={window.Count} avgMs={avg:F3} p95Ms={Percentile(window, 0.95):F3} maxMs={Max(window):F3} " +
                           $"astarPerSec={(sim.AStarCount - astarW0) / span:F1} bfsPerSec={(sim.BfsCount - bfsW0) / span:F1} " +
                           $"fps={(dtW > 0 ? framesW / dtW : 0):F1} maxTicksFrame={maxTicksW} creatures={sim.State.Creatures.Count}";
                Debug.Log(LastLine);
                window.Clear();
                windowStart = now;
                astarW0 = sim.AStarCount;
                bfsW0 = sim.BfsCount;
                framesW = 0;
                dtW = 0;
                maxTicksW = 0;
            }

            if (now - start >= totalSeconds)
            {
                double span = Math.Max(now - start, 1e-9);
                all.Sort();
                double p95 = Percentile(all, 0.95);
                int n = Math.Max(all.Count, 1);
                string phases = "";
                for (int k = 0; k < Simulation.PhaseNames.Length; k++)
                {
                    double us = sim.PhaseTicks[k] * 1e6 / Stopwatch.Frequency / n;
                    phases += (k > 0 ? " " : "") + $"{Simulation.PhaseNames[k]}={us:F1}";
                }
                long produce = sim.State.Stock.ProduceTotal, seeds = sim.State.Stock.SeedsTotal;
                Summary = $"SIMBENCH TONG device={UnityEngine.SystemInfo.deviceModel} cpu={UnityEngine.SystemInfo.processorType} x{UnityEngine.SystemInfo.processorCount} " +
                          $"seconds={now - start:F0} ticks={all.Count} avgMs={Average(all):F3} p95Ms={p95:F3} p99Ms={Percentile(all, 0.99):F3} maxMs={Max(all):F3} " +
                          $"astarPerSec={(sim.AStarCount - astar0) / span:F1} bfsPerSec={(sim.BfsCount - bfs0) / span:F1} " +
                          $"fps={(dtAll > 0 ? framesAll / dtAll : 0):F1} maxTicksFrame={maxTicksAll} pass5ms={p95 <= 5.0} | phaseUs {phases} | produce={produce} seeds={seeds}";
                LastLine = Summary;
                Debug.Log(Summary);
                Done = true;
            }
        }

        public static double Percentile(List<double> sorted, double q)
        {
            if (sorted.Count == 0) return 0;
            int idx = Math.Max(0, (int)Math.Ceiling(q * sorted.Count) - 1);
            return sorted[Math.Min(idx, sorted.Count - 1)];
        }

        static double Average(List<double> l)
        {
            if (l.Count == 0) return 0;
            double s = 0;
            foreach (var v in l) s += v;
            return s / l.Count;
        }

        static double Max(List<double> sorted) => sorted.Count == 0 ? 0 : sorted[sorted.Count - 1];
    }
}
