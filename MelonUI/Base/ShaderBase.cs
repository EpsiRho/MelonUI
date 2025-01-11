using ComputeSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    public class ShaderBase
    {
        public IComputeShader shader { get; set; }

        public ShaderBase()
        {

        }
        public ShaderBase(IComputeShader s)
        {
            shader = s;
        }
    }
}
