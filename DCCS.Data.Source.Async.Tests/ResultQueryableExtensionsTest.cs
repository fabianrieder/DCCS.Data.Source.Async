using System;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DCCS.Data.Source.Async.Tests
{

    [TestFixture]
    public class ResultQueryableExtensionsTest
    {

        public ResultQueryableExtensionsTest()
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
        public async Task Should_create_Result()
        {
            var total = 100;
            await CreateData<Dummy>(total);

            var dummies = DummyContext.Dummies.AsQueryable();
            var sut = await dummies.ToAsyncResult(new Params());

            Assert.AreEqual(total, sut.Total);
        }

      
    }
}
