using System;
using System.Collections.Generic;

namespace PerfTestNhibernate
{
    public class Employee
    {

        public Employee()
        {
            Addresses = new List<Address>();
        }

        public virtual Guid Id { get; set; }

        public virtual string FirstName { get; set; }

        public virtual string LastName { get; set; }

        public virtual IList<Address> Addresses { get; set; }

        public virtual string PhoneNumber { get; set; }

        public override bool Equals(object obj)
        {
            Employee otherEmployee = obj as Employee;
            
            if (otherEmployee == null)
                return false;
            return this.Id == otherEmployee.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
