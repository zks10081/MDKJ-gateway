using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace getway.Model
{
    internal class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public bool IsSave { get; set; } = false;
    }
}
