using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BindingAttribute : Attribute
    {
        public BindingAttribute()
        {

        }
    }
}
