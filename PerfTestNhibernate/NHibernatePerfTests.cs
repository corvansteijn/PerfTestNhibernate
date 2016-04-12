using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Conventions;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Transform;
using Xunit;
using Xunit.Abstractions;

namespace PerfTestNhibernate
{
    public class NHibernatePerfTests
    {
        public const string TableName = "Employee";
        private const int RowCount = 100000;
        private const int repeat = 20;
        private const string DbFile = "hugeSet.db";
        private readonly ITestOutputHelper output;

        public NHibernatePerfTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void StatefulSession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat(() =>
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
            });
        }

        private void Repeat(Func<IEnumerable<EmployeeDto>> action)
        {
            TimeSpan totalTime = TimeSpan.Zero;
            float totalTimeInGc = 0;
            for (var i = 1; i < repeat + 1; i++)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                var employees = action();
                stopwatch.Stop();
                totalTime = totalTime.Add(stopwatch.Elapsed);
                employees.Should().HaveCount(RowCount);
                float timeInGc = PerfCounters.GetPerformanceCounterValue(".NET CLR Memory", "% Time in GC");
                totalTimeInGc += timeInGc;
                output.WriteLine(string.Format("Finished run {0} in {1}, %GC: {2}", i, stopwatch.Elapsed, timeInGc));
            }

            output.WriteLine(string.Format("Total time: {0} average: {1} %GC: {2}", totalTime, new TimeSpan(totalTime.Ticks/repeat), totalTimeInGc/repeat));
        }

        [Fact]
        public void StatefulSessionCustomTypes()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory(new[] { new StringTypeConvention() });

            Repeat(() =>
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
            });
        }

        [Fact]
        public void StatefulSessionWrapResultSets()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory(
                cfg => { cfg.Properties["adonet.wrap_result_sets"] = "true"; },
                new[] {new StringTypeConvention()});

            Repeat(() =>
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
            });
        }

        [Fact]
        public void StatelessSession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat(() =>
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
            });
        }

        [Fact]
        public void SqlSession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat(() =>
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
                            .AddEntity("Employee", typeof(Employee))
                            .AddJoin("Address", "Employee.Addresses")
                            .AddEntity("Employee", typeof(Employee))
                            .SetResultTransformer(new EqualIdentityDistinctRootTransformer())
                            .List<Employee>());
                    }
                }
            });
        }

        [Fact]
        public void ReadonlySession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat(() =>
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
            });
        }

        [Fact]
        public void HqlSession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat(() =>
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
            });
        }

        private static ISessionFactory CreateSessionFactory(IPropertyConvention[] customConventions)
        {
            return CreateSessionFactory(null, customConventions);
        }

        private static ISessionFactory CreateSessionFactory(
            Action<Configuration> exposeConfiguration = null,
            IPropertyConvention[] customConventions = null)
        {
            exposeConfiguration = exposeConfiguration ?? (cfg => { });
            customConventions = customConventions ?? new IPropertyConvention[0];

            return Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2008
                    .ConnectionString("Data Source=.;Initial Catalog=Dingen; Trusted_Connection=yes;"))
                .Mappings(m =>
                {
                    m.FluentMappings.AddFromAssemblyOf<NHibernatePerfTests>();

                    foreach (var convention in customConventions)
                    {
                        m.FluentMappings.Conventions.Add(convention);
                    }
                })
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
}