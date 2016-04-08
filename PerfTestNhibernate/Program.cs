using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Transform;
using Xunit;

namespace PerfTestNhibernate
{
    public class Program
    {
        public const string TableName = "Employee";
        private const int RowCount = 100000;
        private const int Repeat = 20;
        private const string DbFile = "hugeSet.db";

        //[Fact]
        public void All()
        {
            for (var i = 0; i < 1; i++)
            {
                StatefulSession();
                StatelessSession();
                SqlSession();
            }
        }

        [Fact]
        public void StatefulSession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            for (var i = 0; i < Repeat; i++)
            {
                var employees = ReadEmployeesUsingStatefulSession(sessionFactory);
                employees.Should().HaveCount(RowCount);
            }
        }

        [Fact]
        public void StatelessSession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            for (var i = 0; i < Repeat; i++)
            {
                var employees = ReadEmployeesUsingStatelessSession(sessionFactory);
                employees.Should().HaveCount(RowCount);
            }
        }

        [Fact]
        public void SqlSession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            for (var i = 0; i < Repeat; i++)
            {
                var employees = ReadEmployeeUsingSQL(sessionFactory);
                employees.Should().HaveCount(RowCount);
            }
        }

        [Fact]
        public void ReadonlySession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            for (var i = 0; i < Repeat; i++)
            {
                var employees = ReadEmployeesUsingReadonlySession(sessionFactory);
                employees.Should().HaveCount(RowCount);
            }
        }

        [Fact]
        public void HqlSession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            for (var i = 0; i < Repeat; i++)
            {
                var employees = ReadEmployeeUsingHQL(sessionFactory);
                employees.Should().HaveCount(RowCount);
            }
        }

        private static List<Employee> CreateEmployeesForTest()
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

                var addresses = new List<Address>();

                addresses.Add(new Address
                {
                    StreetName = "De Poorterstraat " + i,
                    HouseNumber = i
                    //EmployeeId = employee
                });

                addresses.Add(new Address
                {
                    StreetName = "Vijverberg " + i,
                    HouseNumber = i
                    //EmployeeId = employee
                });

                employee.Addresses = addresses;

                employeeList.Add(employee);
            }
            return employeeList;
        }

        private static IEnumerable<EmployeeDto> ReadEmployeesUsingReadonlySession(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                session.DefaultReadOnly = true;
                ;
                // retreive all stores and display them
                using (session.BeginTransaction())
                {
                    return EmployeeDto.ToDtos(session.Query<Employee>()
                        .Fetch(emp => emp.Addresses)
                        .ToList());
                }
            }
        }

        private static IEnumerable<EmployeeDto> ReadEmployeesUsingStatefulSession(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                // retreive all stores and display them
                using (session.BeginTransaction())
                {
                    return EmployeeDto.ToDtos(session.Query<Employee>()
                        .Fetch(emp => emp.Addresses)
                        .ToList());
                }
            }
        }

        private static IEnumerable<EmployeeDto> ReadEmployeesUsingStatelessSession(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenStatelessSession())
            {
                // retreive all stores and display them
                using (session.BeginTransaction())
                {
                    return EmployeeDto.ToDtos(session.QueryOver<Employee>()
                        .Fetch(emp => emp.Addresses)
                        .Eager
                        .TransformUsing(new EqualIdentityDistinctRootTransformer())
                        .List());
                }
            }
        }

        private static IEnumerable<EmployeeDto> ReadEmployeeUsingSQL(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                // retreive all stores and display them
                using (session.BeginTransaction())
                {
                    var sqlQuery = string.Format(
                        @"select Employee.Id, Employee.FirstName, Employee.LastName, Employee.PhoneNumber, Address.Id, Address.StreetName, Address.HouseNumber, Address.Employee_id
                          from [{0}] Employee
                          left outer join [Address] on Employee.Id = Address.Employee_id", TableName);
                    return EmployeeDto.ToDtos(session.CreateSQLQuery(sqlQuery)
                        .AddEntity("Employee", typeof (Employee))
                        .AddJoin("Address", "Employee.Addresses")
                        .AddEntity("Employee", typeof (Employee))
                        .SetResultTransformer(new EqualIdentityDistinctRootTransformer())
                        .List<Employee>());
                }
            }
        }

        private static IEnumerable<EmployeeDto> ReadEmployeeUsingHQL(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                // retreive all stores and display them
                using (session.BeginTransaction())
                {
                    var hqlQuery =
                        @"select emp from Employee as emp
                          join fetch emp.Addresses";

                    return EmployeeDto.ToDtos(session.CreateQuery(hqlQuery)
                        .SetResultTransformer(Transformers.DistinctRootEntity)
                        .List<Employee>());
                }
            }
        }

        private static void SaveEmployeesToDB(ISessionFactory sessionFactory, List<Employee> employeeList)
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

        private static ISessionFactory CreateSessionFactory(Action<Configuration> exposeConfiguration = null)
        {
            exposeConfiguration = exposeConfiguration ?? (cfg => { });

            return Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2008
                    .ConnectionString("Data Source=.;Initial Catalog=Dingen; Trusted_Connection=yes;"))
                .Mappings(m =>
                    m.FluentMappings.AddFromAssemblyOf<Program>())
                .ExposeConfiguration(exposeConfiguration)
                //.ExposeConfiguration(BuildSchema)
                .BuildSessionFactory();
        }

        private static void BuildSchema(Configuration config)
        {
            // delete the existing db on each run
            //if (File.Exists(DbFile))
            //    File.Delete(DbFile);

            // this NHibernate tool takes a configuration (with mapping info in)
            // and exports a database schema from it
            var schemaExport = new SchemaExport(config);
            schemaExport.Create(true, true);
        }

        public class EmployeeDto
        {
            public IEnumerable<AddressDto> addresses;
            public string firstName;
            public Guid guid;
            public string lastName;
            public string phoneNumber;

            public EmployeeDto(Employee employee)
            {
                addresses = employee.Addresses.Select(a => new AddressDto(a)).ToArray();
                firstName = employee.FirstName;
                guid = employee.Id;
                lastName = employee.LastName;
                phoneNumber = employee.PhoneNumber;
            }

            public static IEnumerable<EmployeeDto> ToDtos(IEnumerable<Employee> employees)
            {
                return employees.Select(e => new EmployeeDto(e)).ToArray();
            }
        }

        public class AddressDto
        {
            public Guid guid;
            public int houseNumber;
            public string streetName;

            public AddressDto(Address address)
            {
                guid = address.Id;
                houseNumber = address.HouseNumber;
                streetName = address.StreetName;
            }
        }
    }


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