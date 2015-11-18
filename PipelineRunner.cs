using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CloserDataPipeline
{
    public class PipelineRunner
    {
        public List<IPipelineStep> Steps { get; protected set; }

        public PipelineRunner()
        {
            Steps = new List<IPipelineStep>();
        }

        public void Run()
        {
            Trace.WriteLine("Executing steps...");
            Trace.WriteLineIf(Steps.Count == 0, "No steps have been added.");

            var workingSet = new List<IVersionable>();

            foreach (var step in Steps)
            {
                try
                {
                    Trace.WriteLine(" start " + step.Name);
                    step.WorkingSet = workingSet;
                    step.Execute();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error: " + ex.Message);
                }
            }

            // Add all the items in the working set to the repository.
            Trace.WriteLine("Registering all items in the working set with the repository...");            
            var client = Utility.GetClient();
            //client.RegisterItems(workingSet, new CommitOptions());
            //Trace.WriteLine(" done registering items");
        }
    }
}
