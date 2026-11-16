// Loading-screen state machine. Pure C# so every rule is unit-tested without Unity.
// Rules (from the handoff): progress is REAL (weighted steps), 100% only when every required step is done, the bar never
// runs ahead of real progress, the Close (X) button works at any moment, and the lobby is reached by auto-advance OR by X.
using System;
using System.Collections.Generic;

namespace NextPlay.Core
{
    public enum LoadingView { Intro, Lobby }

    public sealed class LoadingFlow
    {
        sealed class Step { public string Name; public double Weight, Fraction; }
        readonly List<Step> steps = new List<Step>();

        public double Elapsed { get; private set; }
        public bool CloseRequested { get; private set; }
        public LoadingView View { get; private set; }

        public void AddStep(string name, double weight)
        {
            if (weight <= 0) throw new ArgumentOutOfRangeException("weight");
            if (steps.Exists(s => s.Name == name)) throw new InvalidOperationException("duplicate step " + name);
            steps.Add(new Step { Name = name, Weight = weight });
        }

        Step Find(string name)
        {
            Step s = steps.Find(x => x.Name == name);
            if (s == null) throw new InvalidOperationException("unknown loading step " + name);
            return s;
        }

        // Progress of a step never goes backwards.
        public void Report(string name, double fraction)
        {
            Step s = Find(name);
            double f = Math.Max(0.0, Math.Min(1.0, double.IsNaN(fraction) ? 0.0 : fraction));
            if (f > s.Fraction) s.Fraction = f;
        }
        public void Complete(string name) { Report(name, 1.0); }

        public double Progress
        {
            get
            {
                double total = 0, done = 0;
                foreach (Step s in steps) { total += s.Weight; done += s.Weight * s.Fraction; }
                return total <= 0 ? 0 : done / total;
            }
        }

        public bool AllComplete { get { if (steps.Count == 0) return false; foreach (Step s in steps) if (s.Fraction < 1.0) return false; return true; } }

        // Text shown to the player: 1..100. It reads 100 only when AllComplete is true.
        public static int PercentFor(double shown, bool allComplete)
        {
            if (allComplete && shown >= 0.9999) return 100;
            int p = (int)Math.Floor(shown * 100.0);
            return Math.Max(1, Math.Min(99, p));
        }

        // Bar animation: eases toward the real progress, never above it, never backwards.
        public static double SmoothDisplay(double shown, double target, double dt)
        {
            double t = Math.Max(0, Math.Min(1, target));
            double next = shown + (t - shown) * (1.0 - Math.Exp(-8.0 * Math.Max(0, dt)));
            if (t - next < 0.0005) next = t;          // snap the last sliver so the bar can finish
            return Math.Max(shown, Math.Min(t, next));
        }

        // Call once per frame. Returns true on the frame the view switches to the lobby.
        public bool Tick(double dt, double minHoldSeconds)
        {
            Elapsed += Math.Max(0, dt);
            if (View == LoadingView.Lobby) return false;
            if (CloseRequested || (AllComplete && Elapsed >= minHoldSeconds)) { View = LoadingView.Lobby; return true; }
            return false;
        }

        // X button: allowed at ANY time, never blocked. Loading of required assets continues behind the lobby.
        public void RequestClose() { CloseRequested = true; }

        // The lobby shows, but "Play" stays disabled until every required asset is really loaded.
        public bool LobbyPlayEnabled { get { return View == LoadingView.Lobby && AllComplete; } }
    }
}
