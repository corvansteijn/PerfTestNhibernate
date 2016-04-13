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

        public List<float> Values
        {
            get { return values; }
        }

        public string Category { get; private set; }
        public string Counter { get; private set; }

        private string Average(Func<float, string> toString)
        {
            return SafeTrim(values => values.Average(), toString);
        }

        private string Max(Func<float, string> toString)
        {
            return SafeTrim(values => values.Max(), toString);
        }

        private string Min(Func<float, string> toString)
        {
            return SafeTrim(values => values.Min(), toString);
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

        public string ToShortCsvString()
        {
            if (IsCounter)
            {
                return string.Format("{0}-{1}\t\t\t\t{2}", Category, Counter, Count());
            }

            return string.Format("{0}-{1}\t{2}\t{3}\t{4}\t", Category, Counter, Min(toString), Average(toString), Max(toString));
        }

        public string ToCsvString()
        {
            return string.Format("{0}\t{1}\t{2}", Category, Counter, string.Join("\t", values.Select(v => toString(v)).ToArray()));
        }

        private string ToString(float value)
        {
            return unit.Length == 0 ? toString(value) : string.Format("{0} {1}", toString(value), unit);
        }

        private string SafeTrim(Func<IEnumerable<float>, float> function, Func<float, string> toString)
        {
            var trim = Trim();
            return trim.Any() ? toString(function(trim)) : empty;
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

        private List<float> Trim()
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
            Values.Add(value);
        }
    }
}