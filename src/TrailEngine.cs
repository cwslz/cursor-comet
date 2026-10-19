using System;
using System.Collections.Generic;

namespace MouseTrail
{
    /// <summary>记录鼠标轨迹，维护拖尾采样点与星火粒子。</summary>
    internal sealed class TrailEngine
    {
        public struct Sample
        {
            public float X;
            public float Y;
            public double T;
            public bool Break;
        }

        public sealed class Spark
        {
            public float X;
            public float Y;
            public float VX;
            public float VY;
            public double Life;
            public double MaxLife;
            public float Size;
            public float Hue;
            public float Sat;
            public float Val;
        }

        private const int MaxSamples = 800;

        private readonly List<Sample> _samples = new List<Sample>(MaxSamples);
        private readonly List<Spark> _sparks = new List<Spark>(256);
        private readonly Random _rng = new Random();

        private double _lastSample = -10.0;
        private double _lastUpdate = -1.0;
        private float _lastX;
        private float _lastY;
        private bool _hasLast;
        private float _sparkAccum;

        public float VelocityX { get; private set; }
        public float VelocityY { get; private set; }

        public List<Sample> Samples
        {
            get { return _samples; }
        }

        public List<Spark> Sparks
        {
            get { return _sparks; }
        }

        public void Clear()
        {
            _samples.Clear();
            _sparks.Clear();
            _hasLast = false;
            _lastSample = -10.0;
            _lastUpdate = -1.0;
            _sparkAccum = 0f;
            VelocityX = 0f;
            VelocityY = 0f;
        }

        /// <summary>每帧调用一次，返回当前是否有内容需要绘制。</summary>
        public bool Update(double now, float x, float y, AppSettings cfg, float dpiScale)
        {
            if (_lastUpdate < 0.0)
            {
                _lastUpdate = now;
            }
            double dt = now - _lastUpdate;
            if (dt < 0.0) dt = 0.0;
            if (dt > 0.1) dt = 0.1;
            _lastUpdate = now;

            if (!_hasLast)
            {
                _hasLast = true;
                _lastX = x;
                _lastY = y;
                _lastSample = now;
                return false;
            }

            float dx = x - _lastX;
            float dy = y - _lastY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            bool moved = dist > 0.35f;

            if (moved)
            {
                if (dt > 0.0001)
                {
                    VelocityX = VelocityX * 0.55f + (dx / (float)dt) * 0.45f;
                    VelocityY = VelocityY * 0.55f + (dy / (float)dt) * 0.45f;
                }

                bool brk = false;
                if (_samples.Count > 0)
                {
                    Sample last = _samples[_samples.Count - 1];
                    // 只按“停顿时间”断点。高速移动时相邻采样点距离可能很大，
                    // 但那是连续的轨迹，不能误判成瞬移，否则拖尾中间会断开。
                    if (now - last.T > 0.25)
                    {
                        brk = true;
                    }
                }

                _samples.Add(new Sample { X = x, Y = y, T = now, Break = brk });
                if (_samples.Count > MaxSamples)
                {
                    _samples.RemoveRange(0, _samples.Count - MaxSamples);
                }

                _lastSample = now;
                _lastX = x;
                _lastY = y;

                if (cfg.Style == "sparks")
                {
                    SpawnSparks(x, y, dt, cfg, dpiScale);
                }
            }

            // 只按时间淘汰：鼠标移动越快，拖尾自然越长，不再截断。
            double life = cfg.Life;
            if (life < 0.1)
            {
                life = 0.1;
            }
            while (_samples.Count > 0 && now - _samples[0].T > life)
            {
                _samples.RemoveAt(0);
            }

            UpdateSparks(dt);

            return _samples.Count > 0 || _sparks.Count > 0;
        }

        private void SpawnSparks(float x, float y, double dt, AppSettings cfg, float dpiScale)
        {
            float speed = (float)Math.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY);
            _sparkAccum += (float)dt * (40f + Math.Min(180f, speed * 0.12f)) * (float)cfg.SparkRate;

            int count = (int)_sparkAccum;
            if (count <= 0)
            {
                return;
            }
            if (count > 6)
            {
                count = 6;
            }
            _sparkAccum -= count;

            double baseHue;
            double baseSat;
            double baseVal;
            Palette.ToHsv(Palette.Get(cfg.ColorName), out baseHue, out baseSat, out baseVal);

            for (int i = 0; i < count; i++)
            {
                double angle = _rng.NextDouble() * Math.PI * 2.0;
                double speedRand = (18.0 + _rng.NextDouble() * 70.0) * cfg.SparkSpeed;

                Spark s = new Spark();
                s.X = x + (float)(_rng.NextDouble() * 6.0 - 3.0);
                s.Y = y + (float)(_rng.NextDouble() * 6.0 - 3.0);
                s.VX = -(VelocityX * 0.05f) + (float)(Math.Cos(angle) * speedRand);
                s.VY = -(VelocityY * 0.05f) + (float)(Math.Sin(angle) * speedRand);
                s.MaxLife = (0.45 + _rng.NextDouble() * 0.50) * cfg.SparkLife;
                s.Life = s.MaxLife;
                s.Size = (2.2f + (float)_rng.NextDouble() * 2.8f) * cfg.Scale * dpiScale * (float)cfg.SparkSize;
                s.Hue = (float)(baseHue + (_rng.NextDouble() - 0.5) * 0.14);
                s.Sat = (float)baseSat;
                s.Val = (float)baseVal;
                _sparks.Add(s);
            }

            if (_sparks.Count > 240)
            {
                _sparks.RemoveRange(0, _sparks.Count - 240);
            }
        }

        private void UpdateSparks(double dt)
        {
            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                Spark s = _sparks[i];
                s.Life -= dt;
                if (s.Life <= 0.0)
                {
                    _sparks.RemoveAt(i);
                    continue;
                }

                s.X += s.VX * (float)dt;
                s.Y += s.VY * (float)dt;

                float drag = (float)Math.Exp(-2.0 * dt);
                s.VX *= drag;
                s.VY *= drag;
            }
        }
    }
}
