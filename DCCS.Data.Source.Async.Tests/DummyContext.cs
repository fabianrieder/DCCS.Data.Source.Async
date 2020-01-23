using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace DCCS.Data.Source.Async.Tests
{
    public class DummyContext : DbContext
    {
        public DummyContext(DbContextOptions<DummyContext> options)
            : base(options)
        { }

        public DbSet<Dummy> Dummies { get; set; }
        public DbSet<Secret> Secret { get; set; }

    }
}
