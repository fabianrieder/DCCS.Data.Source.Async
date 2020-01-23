using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core.Exceptions;
using System.Threading.Tasks;
using Bogus;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DCCS.Data.Source.Async.Tests
{

    [TestFixture]
    public class ResultTest
    {

        public ResultTest()
        {
            Randomizer.Seed = new Random(876543);
        }

        public DummyContext DummyContext { get; set; }


        [SetUp]
        public void Init()
        {
            var dbName = $"unittest_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<DummyContext>()
                .UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true",
                    builder =>
                    {
                        
                    })
                //.UseInMemoryDatabase(databaseName: dbName)
                .Options;
            DummyContext = new DummyContext(options);

            DummyContext.Database.EnsureCreated();
            DummyContext.Database.Migrate();
        }

        [TearDown]
        public void Teardown()
        {
            DummyContext.Database.EnsureDeleted();
            DummyContext.Dispose();
        }

        private async Task CreateData<TDummy>(int totalRows) where TDummy : class
        {
            await DummyContext.AddRangeAsync(new Faker<TDummy>().Generate(totalRows));
            await DummyContext.SaveChangesAsync();
        }

        [Test]
        public async Task Should_create_empty_data()
        {
            var sut = await AsyncResult.Create(new Params(), new List<string>().AsQueryable());
            Assert.IsNotNull(sut.Data);
        }

        [Test]
        public async Task Should_return_total()
        {
            var total = 100;
            await CreateData<Dummy>(total);

            var sut = await AsyncResult.Create(new Params(), DummyContext.Dummies);

            Assert.AreEqual(total, sut.Total);
        }

        [Test]
        public async Task Should_return_total_with_paging()
        {
            var total = 100;
            await CreateData<Dummy>(total);

            var sut = await AsyncResult.Create(new Params { Count = 10, Page = 2 }, DummyContext.Dummies);

            Assert.AreEqual(total, sut.Total);
        }

        [Test]
        public async Task Should_return_correct_total_if_count_bigger_than_total()
        {
            var total = 100;
            await CreateData<Dummy>(total);

            var sut = await AsyncResult.Create(new Params { Count = 200, Page = 2 }, DummyContext.Dummies);

            Assert.AreEqual(total, sut.Total);
        }

        [Test]
        public async Task Should_return_correct_page_if_requested_page_not_available()
        {
            var total = 100;
            await CreateData<Dummy>(total);

            var sut = await AsyncResult.Create(new Params { Count = 200, Page = 2 }, DummyContext.Dummies);

            Assert.AreEqual(1, sut.Page);
        }

        [Test]
        public async Task Should_map_data()
        {
            var total = 2;
            await CreateData<Dummy>(total);

            var ps = new Params { Count = 10, Page = 1 };


            var sut = await AsyncResult.Create(ps, DummyContext.Dummies)
                .Select(entry => new DummyDTO {Name = entry.Name, Length = (entry.Name ?? "").Length});

            Assert.IsInstanceOf(typeof(DummyDTO), sut.Data.First());
        }

        [Test]
        public async Task ProjectedData_Should_Have_Correct_Count()
        {
            var total = 200;
            await CreateData<Dummy>(total);

            var ps = new Params { Count = 10, Page = 1 };


            var sut = await AsyncResult.Create(ps, DummyContext.Dummies)
                .Select(entry => new DummyDTO { Name = entry.Name, Length = (entry.Name ?? "").Length });

            Assert.IsInstanceOf(typeof(DummyDTO), sut.Data.First());
            Assert.AreEqual(total, sut.Total);
        }


        [Test]
        //todo: this is failing on mssql server
        public async Task Should_Not_Throw_OnEmptyResult()
        {
            Assert.IsTrue(!DummyContext.Dummies.Any());

            var ps = new Params
            {
                Count = 1, 
                Page = 1,
                OrderBy = nameof(Dummy.Name)
            };

            var data = DummyContext.Dummies.Where(x => x.Name == "foo").AsQueryable();

            var sut = await AsyncResult.Create(ps, data);

            Assert.IsNotNull(sut);
        }

        [Test]

        public async Task Should_Mitigate_LinqInjection()
        {
            Assert.IsTrue(!DummyContext.Dummies.Any());
            var ps = new Params
            {
                OrderBy = @""""".GetType().Assembly.GetType(""System.AppDomain"").GetMethods()[104].Invoke("""".GetType().Assembly.GetType(""System.AppDomain"").GetProperty(""CurrentDomain"").GetValue(null), ""System,Version = 4.0.0.0,Culture = neutral,PublicKeyToken = b77a5c561934e089; System.Diagnostics.Process"".Split(""; "".ToCharArray())).GetType().GetMethods()[80].Invoke(null, ""cmd;/ T:4A / K whoami && echo was HACKED"".Split(""; "".ToCharArray()))"""
            };

            var data = DummyContext.Dummies.AsQueryable();
            Assert.ThrowsAsync<ParseException>(async () => { await AsyncResult.Create(ps, data); });
        }

        [Test]
        public async Task Should_Not_Throw_Sql_Errors()
        {
            Assert.IsTrue(!DummyContext.Dummies.Any());
            var ps = new Params
            {
                OrderBy = @"Name'"
            };

            var data = DummyContext.Dummies.AsQueryable();
            Assert.ThrowsAsync<ParseException>(async () => { await AsyncResult.Create(ps, data); });
        }

        [Test]
        public async Task Should_Mitigate_SqlInjection()
        {
            Assert.IsTrue(!DummyContext.Dummies.Any());
            var ps = new Params
            {
                OrderBy = @"Name,(SELECT 3563 WHERE 3563=CONVERT(INT,(SELECT CHAR(113)+CHAR(122)+CHAR(120)+CHAR(112)+CHAR(113)+(SELECT (CASE WHEN (3563=3563) THEN CHAR(49) ELSE CHAR(48) END))+CHAR(113)+CHAR(98)+CHAR(106)+CHAR(120)+CHAR(113))))"
            };

            var data = DummyContext.Dummies.AsQueryable();
            Assert.ThrowsAsync<ParseException>(async () => { await AsyncResult.Create(ps, data); });
        }


    }
}
