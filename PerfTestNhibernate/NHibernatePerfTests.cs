using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
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
        private const int CounterSleepTime = 1000;
        private int repeat = 1;
        private const string DbFile = "hugeSet.db";
        private readonly ITestOutputHelper output;
        private string outputFile;

        public NHibernatePerfTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        public static NHibernatePerfTests Create(XUnitConsole instance, string file, string repeat)
        {
            return new NHibernatePerfTests(instance)
            {
                outputFile = file,
                repeat = int.Parse(repeat)
            };
        }

        private void Repeat(string scenario, Func<IEnumerable<EmployeeDto>> action)
        {
            MeasurementInfo timeMeasurement = new MeasurementInfo("Custom", "Duration", x => x.ToString("0"));
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var measurements = GetMeasurements(cancellationTokenSource.Token);
            measurements.Add(timeMeasurement);
            
            for (var i = 1; i < repeat + 1; i++)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                var employees = action();
                stopwatch.Stop();
                timeMeasurement.AddValue((float)stopwatch.Elapsed.TotalMilliseconds);
                employees.Should().HaveCount(RowCount);
            }

            cancellationTokenSource.Cancel();

            foreach (var measurement in measurements)
            {
                output.WriteLine(measurement.ToHumanString());
            }

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            path = path.Substring(6);
            //string fileName = SanatizeFileName(string.Format(@"{0} {1}.csv", scenario, DateTime.Now));
            string shortFileName = outputFile ?? SanatizeFileName(string.Format(@"{0} short {1}.csv", scenario, DateTime.Now));
            //File.AppendAllLines(string.Format(@"{0}\{1}", path, fileName), measurements.Select(m => m.ToCsvString()).ToArray());
            File.AppendAllLines(string.Format(@"{0}\{1}", path, shortFileName), measurements.Select(m => m.ToShortCsvString(scenario)).ToArray());
        }

        private static string SanatizeFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(name, invalidRegStr, "");
        }

        private List<MeasurementInfo> GetMeasurements(CancellationToken token)
        {
            List<MeasurementInfo> measurements = new List<MeasurementInfo>
            {
                new MeasurementInfo(".NET CLR Memory", "# Bytes in all Heaps", "MB", x => (x / (1024*1024)).ToString("0.00")),
                new MeasurementInfo(".NET CLR Memory", "# Gen 0 Collections", "#", x => x.ToString("0")),
                new MeasurementInfo(".NET CLR Memory", "# Gen 1 Collections", "#", x => x.ToString("0")),
                new MeasurementInfo(".NET CLR Memory", "# Gen 2 Collections", "#", x => x.ToString("0")),
                new MeasurementInfo(".NET CLR Memory", "Gen 0 heap size", "MB", x => (x / (1024*1024)).ToString("0.00")),
                new MeasurementInfo(".NET CLR Memory", "Gen 1 heap size", "MB", x => (x / (1024*1024)).ToString("0.00")),
                new MeasurementInfo(".NET CLR Memory", "Gen 2 heap size", "MB", x => (x / (1024*1024)).ToString("0.00")),
                new MeasurementInfo(".NET CLR Memory", "Large Object Heap size", "MB", x => (x / (1024*1024)).ToString("0.00")),
                new MeasurementInfo(".NET CLR Memory", "% Time in GC", "%"),
                new MeasurementInfo("Process", "% Processor Time", "%"),
                new MeasurementInfo("Process", "Working Set", "MB", x => (x / (1024*1024)).ToString("0.00"))
            };

            IEnumerable<Action> actions = measurements
                .Select(m => (Action)(() => GetPerformanceCounterValue(m)))
                .ToArray();
                
                GetMeasurements(actions, token, CounterSleepTime);

            return measurements;
        }

        private void GetPerformanceCounterValue(MeasurementInfo measurementInfo)
        {
            measurementInfo.AddValue(PerfCounters.GetPerformanceCounterValue(measurementInfo.Category, measurementInfo.Counter));
        }

        private static void GetMeasurements(IEnumerable<Action> actions, CancellationToken token, int delay)
        {
            var tasks = new List<Task>();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

            foreach (var action in actions)
            {
                var currentAction = action;
                var task = Task.Factory.StartNew(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(delay, cts.Token).ConfigureAwait(false);
                        currentAction();
                    }
                });
                tasks.Add(task);
            }

            Task.WhenAll(tasks);
        }

        [Fact]
        public void StatefulSession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat("Stateful session", () =>
            {
                using (var session = sessionFactory.OpenSession())
                {
                    // retrieve all stores and display them
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
        public void StatefulSessionWithFlush()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat("Stateful session with flush", () =>
            {
                using (var session = sessionFactory.OpenSession())
                {
                    // retrieve all stores and display them
                    using (var transaction = session.BeginTransaction())
                    {
                        var employees = EmployeeDto.ToDtos(session.Query<Employee>()
                            .Fetch(emp => emp.Addresses)
                            .ToList());

                        transaction.Commit();
                        return employees;
                    }
                }
            });
        }

        [Fact]
        public void StatefulSessionWithTransactionScope()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat("Stateful session with transaction scope", () =>
            {
                using (var session = sessionFactory.OpenSession())
                {
                    // retrieve all stores and display them
                    using (var tx = new TransactionScope())
                    {
                        var employees = EmployeeDto.ToDtos(session.Query<Employee>()
                            .Fetch(emp => emp.Addresses)
                            .ToList());

                        tx.Complete();
                        return employees;
                    }
                }
            });
        }

        [Fact]
        public void StatefulSessionWithTransactionScopeNoComplete()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat("Stateful session with transaction scope but no complete", () =>
            {
                using (var session = sessionFactory.OpenSession())
                {
                    // retrieve all stores and display them
                    using (var tx = new TransactionScope())
                    {
                        var employees = EmployeeDto.ToDtos(session.Query<Employee>()
                            .Fetch(emp => emp.Addresses)
                            .ToList());

                        return employees;
                    }
                }
            });
        }

        [Fact]
        public void CustomTupilizer()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory(typeof(CustomTupilizerAddressMap), typeof(CustomTupilizerEmployeeMap));

            Repeat("Set id without reflection", () =>
            {
                using (var session = sessionFactory.OpenSession())
                {
                    // retrieve all stores and display them
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
        public void StatefulSessionCustomTypes()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory(new StringTypeConvention());

            Repeat("Stateful session no IsDbNull", () =>
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

            Repeat("Stateful session wrap resultsets", () =>
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

            Repeat("Stateless session", () =>
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
        public void StatelessSessionWithFlush()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat("Stateless session with flush", () =>
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    // retreive all stores and display them
                    using (var transaction = session.BeginTransaction())
                    {
                        var employees = EmployeeDto.ToDtos(session.QueryOver<Employee>()
                            .Fetch(emp => emp.Addresses)
                            .Eager
                            .TransformUsing(new EqualIdentityDistinctRootTransformer())
                            .List());

                        transaction.Commit();
                        return employees;
                    }
                }
            });
        }

        [Fact]
        public void StatelessSessionWithTransactionScope()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat("Stateless session with transaction scope", () =>
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    // retreive all stores and display them
                    using (var tx = new TransactionScope())
                    {
                        var employees = EmployeeDto.ToDtos(session.QueryOver<Employee>()
                            .Fetch(emp => emp.Addresses)
                            .Eager
                            .TransformUsing(new EqualIdentityDistinctRootTransformer())
                            .List());

                        tx.Complete();
                        return employees;
                    }
                }
            });
        }

        [Fact]
        public void StatelessSessionWithTransactionScopeNoComplete()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat("Stateless session with transaction scope but no complete", () =>
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    // retreive all stores and display them
                    using (var tx = new TransactionScope())
                    {
                        var employees = EmployeeDto.ToDtos(session.QueryOver<Employee>()
                            .Fetch(emp => emp.Addresses)
                            .Eager
                            .TransformUsing(new EqualIdentityDistinctRootTransformer())
                            .List());

                        return employees;
                    }
                }
            });
        }

        [Fact]
        public void SqlSession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat("Sql query", () =>
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

            Repeat("Readonly session", () =>
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
        public void ReadonlySessionWithFlush()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat("Readonly session with flush", () =>
            {
                using (var session = sessionFactory.OpenSession())
                {
                    session.DefaultReadOnly = true;
                    
                    // retreive all stores and display them
                    using (var transaction = session.BeginTransaction())
                    {
                        var employees = EmployeeDto.ToDtos(session.Query<Employee>()
                            .Fetch(emp => emp.Addresses)
                            .ToList());

                        transaction.Commit();
                        return employees;
                    }
                }
            });
        }

        [Fact]
        public void ReadonlySessionWithTransactionScope()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat("Readonly session with transaction scope", () =>
            {
                using (var session = sessionFactory.OpenSession())
                {
                    session.DefaultReadOnly = true;

                    using (var tx = new TransactionScope())
                    {
                        var employees = EmployeeDto.ToDtos(session.Query<Employee>()
                            .Fetch(emp => emp.Addresses)
                            .ToList());

                        tx.Complete();
                        return employees;
                    }
                }
            });
        }

        [Fact]
        public void HqlSession()
        {
            // create our NHibernate session factory
            var sessionFactory = CreateSessionFactory();

            Repeat("Hql query", () =>
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

        private static ISessionFactory CreateSessionFactory(params Type[] customClassMaps)
        {
            return CreateSessionFactory(null, null, customClassMaps);
        }

        private static ISessionFactory CreateSessionFactory(params IPropertyConvention[] customConventions)
        {
            return CreateSessionFactory(null, customConventions);
        }

        private static ISessionFactory CreateSessionFactory(
            Action<Configuration> exposeConfiguration = null,
            IPropertyConvention[] customConventions = null,
            Type[] mappingTypes = null)
        {
            exposeConfiguration = exposeConfiguration ?? (cfg => { });
            customConventions = customConventions ?? new IPropertyConvention[0];
            mappingTypes = mappingTypes ?? new[] { typeof(EmployeeMap), typeof(AddressMap) };

            return Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2008
                    .ConnectionString("Data Source=.;Initial Catalog=Dingen; Trusted_Connection=yes;"))
                .Mappings(m =>
                {
                    foreach (var type in mappingTypes)
                    {
                        m.FluentMappings.Add(type);
                    }

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