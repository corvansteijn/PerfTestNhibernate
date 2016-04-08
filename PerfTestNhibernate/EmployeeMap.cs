using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FluentNHibernate.Mapping;
using FluentNHibernate.MappingModel;
using NHibernate.Mapping;
using NHibernate.Properties;
using NHibernate.Tuple.Entity;
using Property = NHibernate.Mapping.Property;

namespace PerfTestNhibernate
{
    public class EmployeeMap : ClassMap<Employee>
    {
        public EmployeeMap()
        {
            Tuplizer(TuplizerMode.Poco, typeof (EmployeeTupilizer));
            Table(Program.TableName);

            Id(emp => emp.Id);
            Map(emp => emp.FirstName);
            Map(emp => emp.LastName);
            Map(emp => emp.PhoneNumber);

            HasMany(emp => emp.Addresses)
                .Cascade.AllDeleteOrphan();
        }
    }

    public class EmployeeTupilizer : ClassTupilizer<Employee>
    {
        public EmployeeTupilizer(EntityMetamodel entityMetamodel, PersistentClass mappingInfo)
            : base(entityMetamodel, mappingInfo)
        {
        }

        protected override void Build()
        {
            Map(e => e.Id);
        }
    }

    public abstract class ClassTupilizer<T> : PocoEntityTuplizer
    {
        private IDictionary<string, ISetter> settersCache;

        private IDictionary<string, ISetter> SettersCache
        {
            get
            {
                if (settersCache == null)
                {
                    settersCache = new Dictionary<string, ISetter>();
                    Build();
                }

                return settersCache;
            }
        }

        protected abstract void Build();


        protected ClassTupilizer(EntityMetamodel entityMetamodel, PersistentClass mappingInfo)
            : base(entityMetamodel, mappingInfo)
        {
        }

        protected void Map<TProperty>(Expression<Func<T, TProperty>> memberExpression)
        {
            var member = (MemberExpression)memberExpression.Body;
            var param = Expression.Parameter(typeof(TProperty), "value");
            var set = Expression.Lambda<Action<T, TProperty>>(
                Expression.Assign(member, param), memberExpression.Parameters[0], param);
            var setter = new ClassPropertySetter<T, TProperty>(member.Member.Name, set);
            string cackeKey = string.Format("{0}-{1}", setter.PropertyName, typeof (T).FullName);
            settersCache[cackeKey] = setter;
        }

        protected override ISetter BuildPropertySetter(Property mappedProperty,
            PersistentClass mappedEntity)
        {
            string cackeKey = string.Format("{0}-{1}", mappedProperty.Name, typeof(T).FullName);

            ISetter setter;
            if (SettersCache.TryGetValue(cackeKey, out setter))
            {
                return setter;
            }

            return base.BuildPropertySetter(mappedProperty, mappedEntity);
        }
    }

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