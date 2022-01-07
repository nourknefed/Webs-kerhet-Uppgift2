using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Models
{
    public class FileUppload
    {
        public Guid Id { get; set; }
        public string UntrustedName { get; set; }
        public long size { get; set; }
        public byte[] Content { get; set; }
        public DateTime Time { get; set; }
    }
}
