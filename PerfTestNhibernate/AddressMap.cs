using FluentNHibernate.Mapping;

namespace PerfTestNhibernate
{
    public class AddressMap : ClassMap<Address>
    {
        public AddressMap()
        {
            Id(x => x.Id);
            Map(x => x.StreetName);
            Map(x => x.HouseNumber);
            //References(x => x.EmployeeId);
        }
    }
}
