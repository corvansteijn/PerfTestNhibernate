using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NHibernate.Mapping;
using NHibernate.Properties;
using NHibernate.Tuple.Entity;

namespace PerfTestNhibernate
{
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
}