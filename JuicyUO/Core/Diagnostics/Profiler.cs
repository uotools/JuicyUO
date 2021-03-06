﻿#region license
//  Copyright (C) 2018 JuicyUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies such as DirectX (MonoGame included). The foundation
//	is originally licensed (GNU) on JuicyUO and the JuicyUO Development
//	Team. (Copyright (c) 2015 JuicyUO Development Team)
//    
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using JuicyUO.Core.Diagnostics.Tracing;

namespace JuicyUO.Core.Diagnostics
{
    public static class Profiler
    {
        public const int ProfileTimeCount = 60;

        static List<ContextAndTick> m_Context;
        static HighPerformanceTimer m_Timer;
        static List<Tuple<string[], double>> m_ThisFrameData;
        static List<ProfileData> m_AllFrameData;
        static ProfileData m_TotalTimeData;
        static long m_BeginFrameTicks;
        public static double LastFrameTimeMS { get; private set; }
        public static double TrackedTime => m_TotalTimeData.TimeInContext;

        static Profiler()
        {
            m_Context = new List<ContextAndTick>();
            m_ThisFrameData = new List<Tuple<string[], double>>();
            m_AllFrameData = new List<ProfileData>();
            m_TotalTimeData = new ProfileData(null, 0d);
            m_Timer = new HighPerformanceTimer();
            m_Timer.Start();
        }

        public static void BeginFrame()
        {
            if (m_ThisFrameData.Count > 0)
            {
                for (int i = 0; i < m_ThisFrameData.Count; i++)
                {
                    bool added = false;
                    for (int j = 0; j < m_AllFrameData.Count; j++)
                    {
                        if (m_AllFrameData[j].MatchesContext(m_ThisFrameData[i].Item1))
                        {
                            m_AllFrameData[j].AddNewHitLength(m_ThisFrameData[i].Item2);
                            added = true;
                            break;
                        }
                    }
                    if (!added)
                    {
                        m_AllFrameData.Add(new ProfileData(m_ThisFrameData[i].Item1, m_ThisFrameData[i].Item2));
                    }
                }
                m_ThisFrameData.Clear();
            }

            m_BeginFrameTicks = m_Timer.ElapsedTicks;
        }

        public static void EndFrame()
        {
            LastFrameTimeMS = HighPerformanceTimer.SecondsFromTicks(m_Timer.ElapsedTicks - m_BeginFrameTicks) * 1000d;
            m_TotalTimeData.AddNewHitLength(LastFrameTimeMS);
        }

        public static void EnterContext(string context_name)
        {
            m_Context.Add(new ContextAndTick(context_name, m_Timer.ElapsedTicks));
        }

        public static void ExitContext(string context_name)
        {
            if (m_Context[m_Context.Count - 1].Name != context_name)
                Tracer.Error("Profiler.ExitProfiledContext: context_name does not match current context.");
            string[] context = new string[m_Context.Count];
            for (int i = 0; i < m_Context.Count; i++)
                context[i] = m_Context[i].Name;

            double ms = HighPerformanceTimer.SecondsFromTicks(m_Timer.ElapsedTicks - m_Context[m_Context.Count - 1].Tick) * 1000d;
            m_ThisFrameData.Add(new Tuple<string[], double>(context, ms));
            m_Context.RemoveAt(m_Context.Count - 1);
        }

        public static bool InContext(string context_name)
        {
            if (m_Context.Count == 0)
                return false;
            return (m_Context[m_Context.Count - 1].Name == context_name);
        }

        public static ProfileData GetContext(string context_name)
        {
            for (int i = 0; i < m_AllFrameData.Count; i++)
                if (m_AllFrameData[i].Context[m_AllFrameData[i].Context.Length - 1] == context_name)
                    return m_AllFrameData[i];
            return ProfileData.Empty;
        }

        public class ProfileData
        {
            public string[] Context;
            double[] m_LastTimes = new double[ProfileTimeCount];
            uint m_LastIndex;

            public double LastTime => m_LastTimes[m_LastIndex % ProfileTimeCount];

            public double TimeInContext
            {
                get
                {
                    double time = 0;
                    for (int i = 0; i < ProfileTimeCount; i++)
                    {
                        time += m_LastTimes[i];
                    }
                    return time;
                }
            }

            public double AverageTime => TimeInContext / ProfileTimeCount;

            public ProfileData(string[] context, double time)
            {
                Context = context;
                m_LastIndex = 0;
                AddNewHitLength(time);
            }

            public bool MatchesContext(string[] context)
            {
                if (Context.Length != context.Length)
                    return false;
                for (int i = 0; i < Context.Length; i++)
                    if (Context[i] != context[i])
                        return false;
                return true;
            }

            public void AddNewHitLength(double time)
            {
                m_LastTimes[m_LastIndex % ProfileTimeCount] = time;
                m_LastIndex++;
            }

            public override string ToString()
            {
                string name = string.Empty;
                for (int i = 0; i < Context.Length; i++)
                {
                    if (name != string.Empty)
                        name += ":";
                    name += Context[i];
                }
                return $"{name} - {TimeInContext:0.0}ms";
            }

            public static ProfileData Empty = new ProfileData(null, 0d);
        }

        private struct ContextAndTick
        {
            public readonly string Name;
            public readonly long Tick;

            public ContextAndTick(string name, long tick)
            {
                Name = name;
                Tick = tick;
            }

            public override string ToString()
            {
                return string.Format("{0} [{1}]", Name, Tick);
            }
        }
    }
}
