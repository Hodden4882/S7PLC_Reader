using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiemensDemo.Models
{
    public class PlcDataModel
    {
        public bool TestBool { get; set; }
        public byte TestByte { get; set; }
        public ushort TestWord { get; set; }
        public uint TestDWord { get; set; }
        public short TestInt { get; set; }
        public int TestDint { get; set; }
        public float TestReal { get; set; }
        public string TestString { get; set; }
    }
}
