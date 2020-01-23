using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DCCS.Data.Source.Async.Tests
{
    public class Secret
    {
        [Key]
        public int Id { get; set; }
        public string Password { get; set; }
    }
}
