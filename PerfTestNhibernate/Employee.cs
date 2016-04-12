using System;
using System.Collections.Generic;
using NHibernate;

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

        public static List<Employee> CreateEmployeesForTest()
        {
            var employeeList = new List<Employee>();
            for (var i = 0; i < 100000; i++)
            {
                var employee = new Employee
                {
                    FirstName = "HUGEFIRSTNAME" + i,
                    LastName = "HUGELASTNAME" + i,
                    PhoneNumber = "Lovely phone number " + i
                };

                var addresses = new List<Address>
                {
                    new Address
                    {
                        StreetName = "De Poorterstraat " + i,
                        HouseNumber = i
                        //EmployeeId = employee
                    },
                    new Address
                    {
                        StreetName = "Vijverberg " + i,
                        HouseNumber = i
                        //EmployeeId = employee
                    }
                };

                employee.Addresses = addresses;

                employeeList.Add(employee);
            }
            return employeeList;
        }

        public static void SaveEmployeesToDB(ISessionFactory sessionFactory, List<Employee> employeeList)
        {
            using (var session = sessionFactory.OpenSession())
            {
                // populate the database
                using (var transaction = session.BeginTransaction())
                {
                    foreach (var employee in employeeList)
                    {
                        session.Save(employee);
                    }
                    // save both stores, this saves everything else via cascading
                    //session.SaveOrUpdate(barginBasin);

                    transaction.Commit();
                }
            }
        }
    }
}
