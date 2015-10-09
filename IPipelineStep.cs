using Algenta.Colectica.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloserDataPipeline
{
    public interface IPipelineStep
    {
        string Name { get; }

        List<IVersionable> WorkingSet { get; set; }

        void Execute();
    }
}
