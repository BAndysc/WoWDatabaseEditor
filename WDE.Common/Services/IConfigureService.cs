using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IConfigureService
    {
        void ShowSettings();
    }
}
