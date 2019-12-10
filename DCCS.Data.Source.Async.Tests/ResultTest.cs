using Bogus;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCCS.Data.Source.Async.Tests;
using Microsoft.EntityFrameworkCore;

namespace DCCS.Data.Source.Tests
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
            var options = new DbContextOptionsBuilder<DummyContext>()
                .UseInMemoryDatabase(databaseName: $"test{Guid.NewGuid()}")
                .Options;
            DummyContext = new DummyContext(options);
        }

        [TearDown]
        public void Teardown()
        {
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
    }
}
