using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Transform;

namespace PerfTestNhibernate
{
    public class EqualIdentityDistinctRootTransformer : IResultTransformer
    {
        public object TransformTuple(object[] tuple, string[] aliases)
        {
            return tuple[tuple.Length - 1];
        }

        public IList TransformList(IList list)
        {
            var list1 = (IList) Activator.CreateInstance(list.GetType());

            var hashSet = new HashSet<Identity>();

            foreach (var entity in list)
            {
                if (hashSet.Add(new Identity(entity)))
                    list1.Add(entity);
            }

            return list1;
        }

        internal sealed class Identity
        {
            internal readonly object entity;

            internal Identity(object entity)
            {
                this.entity = entity;
            }

            public override bool Equals(object other)
            {
                return Equals(entity, ((Identity) other).entity);
            }

            public override int GetHashCode()
            {
                //return RuntimeHelpers.GetHashCode(this.entity);
                return entity.GetHashCode();
            }
        }
    }
}