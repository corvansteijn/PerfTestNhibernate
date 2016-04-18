using System;
using System.Collections.Generic;
using System.Linq;

namespace PerfTestNhibernate
{
    public class MeasurementInfo
    {
        private readonly string unit;
        private readonly Func<float, string> toString;
        private readonly List<float> values = new List<float>();
        private const string empty = "Empty";

        public MeasurementInfo(string category, string counter, Func<float, string> toString = null)
            : this(category, counter, string.Empty, toString)
        {
        }

        public MeasurementInfo(string category, string counter, string unit, Func<float, string> toString = null)
        {
            this.unit = unit;
            this.toString = toString ?? (x => x.ToString("0.00"));
            Category = category;
            Counter = counter;
        }

        public string Category { get; private set; }
        public string Counter { get; private set; }

        private string Average(Func<float, string> toString)
        {
            return SafeToString(Trim(), values => values.Average(), toString);
        }

        private string Max(Func<float, string> toString)
        {
            return SafeToString(values, v => v.Max(), toString);
        }

        private string Min(Func<float, string> toString)
        {
            return SafeToString(values, v => v.Min(), toString);
        }

        public string ToHumanString()
        {
            if (IsCounter)
            {
                return string.Format("{0}-{1} #: {2}", Category, Counter, Count());
            }

            return string.Format("{0}-{1} min: {2} average: {3} max: {4}", Category, Counter, Min(ToString), Average(ToString), Max(ToString));
        }

        private bool IsCounter
        {
            get { return unit == "#"; }
        }

        public string ToShortCsvString(string scenario)
        {
            if (IsCounter)
            {
                return string.Format("{3}\t{0}-{1}\t\t\t\t{2}", Category, Counter, Count(), scenario);
            }

            return string.Format("{5}\t{0}-{1}\t{2}\t{3}\t{4}\t", Category, Counter, Min(toString), Average(toString), Max(toString), scenario);
        }

        public string ToCsvString()
        {
            return string.Format("{0}\t{1}\t{2}", Category, Counter, string.Join("\t", values.Select(v => toString(v)).ToArray()));
        }

        private string ToString(float value)
        {
            return unit.Length == 0 ? toString(value) : string.Format("{0} {1}", toString(value), unit);
        }

        private string SafeToString(IEnumerable<float> values, Func<IEnumerable<float>, float> function, Func<float, string> toString)
        {
            return values.Any() ? toString(function(values)) : empty;
        }

        private int Count()
        {
            if (values.Any())
            {
                float initial = values.First();
                return (int)values
                    .Select(v => v - initial)
                    .Last();
            }

            return 0;
        }

        private IEnumerable<float> Trim()
        {
            int trimBy = (int)Math.Floor(values.Count * 0.1);
            return values
                .OrderBy(x => x)
                .Skip(trimBy)
                .Take(values.Count - trimBy * 2)
                .ToList();
        }

        public void AddValue(float value)
        {
            values.Add(value);
        }
    }
}