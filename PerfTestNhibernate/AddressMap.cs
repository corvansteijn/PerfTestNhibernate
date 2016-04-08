using FluentNHibernate.Mapping;
using FluentNHibernate.MappingModel;
using NHibernate.Mapping;
using NHibernate.Tuple.Entity;

namespace PerfTestNhibernate
{
    public class AddressMap : ClassMap<Address>
    {
        public AddressMap()
        {
            Tuplizer(TuplizerMode.Poco, typeof(AddressTupilizer));
            Id(x => x.Id);
            Map(x => x.StreetName);
            Map(x => x.HouseNumber);
            //References(x => x.EmployeeId);
        }
    }

    public class AddressTupilizer : ClassTupilizer<Address>
    {
        public AddressTupilizer(EntityMetamodel entityMetamodel, PersistentClass mappingInfo)
            : base(entityMetamodel, mappingInfo)
        {
        }

        protected override void Build()
        {
            Map(e => e.Id);
        }
    }
}
