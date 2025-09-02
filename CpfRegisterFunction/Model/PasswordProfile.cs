using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CpfRegisterFunction.Model
{
    public class PasswordProfile
    {
        public bool ForceChangePasswordNextSignIn { get; set; } = true;
        public string Password { get; set; } = "SenhaForte123";
    }
}
