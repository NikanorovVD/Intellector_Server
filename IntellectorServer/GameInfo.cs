using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntellectorServer
{
    public class GameInfo
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public ColorChoice ColorChoice { get; set; }
        public TimeContol TimeContol { get; set; }

        public GameInfo() { }

        public override string ToString()
        {
            return $"ID = {ID}, Name = {Name}, Color = {ColorChoice}, Time = {TimeContol}";
        }
    }
}
