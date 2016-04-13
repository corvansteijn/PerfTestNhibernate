using FluentNHibernate.Mapping;
using FluentNHibernate.MappingModel;
using NHibernate.Mapping;
using NHibernate.Tuple.Entity;

namespace PerfTestNhibernate
{
    public class EmployeeMap : ClassMap<Employee>
    {
        public EmployeeMap()
        {
            Table(NHibernatePerfTests.TableName);

            Id(emp => emp.Id);
            Map(emp => emp.FirstName);
            Map(emp => emp.LastName);
            Map(emp => emp.PhoneNumber);

            HasMany(emp => emp.Addresses)
                .Cascade.AllDeleteOrphan();
        }
    }

    public class CustomTupilizerEmployeeMap : EmployeeMap
    {
        public CustomTupilizerEmployeeMap()
        {
            Tuplizer(TuplizerMode.Poco, typeof(EmployeeTupilizer));
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
}