using System;
using System.Linq.Expressions;
using System.Reflection;
using NHibernate.Properties;

namespace PerfTestNhibernate
{
    public class ClassPropertySetter<TInstance, TProperty> : ISetter
    {
        private readonly string name;
        private readonly Action<TInstance, TProperty> set;

        public ClassPropertySetter(string name, Expression<Action<TInstance, TProperty>> set)
        {
            this.name = name;
            this.set = set.Compile();
        }

        public void Set(object target, object value)
        {
            var instance = (TInstance)target;
            var propertyValue = (TProperty)value;
            set(instance, propertyValue);
        }

        public string PropertyName
        {
            get { return name; }
        }

        public MethodInfo Method 
        {
            get { return typeof (Employee).GetProperty("Id").SetMethod; }
        }
    }
}