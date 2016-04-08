using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;

namespace PerfTestNhibernate
{
    public class StringTypeConvention : IPropertyConvention
    {
        public void Apply(IPropertyInstance instance)
        {
            if (instance.Type.GetUnderlyingSystemType() == typeof(string))
                instance.CustomType<StringType>();
        }
    }
}