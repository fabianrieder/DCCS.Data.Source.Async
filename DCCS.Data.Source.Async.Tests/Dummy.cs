using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DCCS.Data.Source.Async.Tests
{
    public class Dummy
    {
        [Key]
        public string Name { get; set; }
    }
}
