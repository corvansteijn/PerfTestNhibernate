using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfTestNhibernate
{
    public class Address
    {
        public virtual Guid Id { get; set; }
        public virtual string StreetName { get; set; }
        public virtual int HouseNumber { get; set; }
        //public virtual Employee EmployeeId { get; set; }
    }
}
