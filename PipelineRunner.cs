using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (Steps.Count == 0)
            {
                Console.WriteLine("No steps have been added.");
            }

            var workingSet = new List<IVersionable>();

            foreach (var step in Steps)
            {
                try
                {
                    Console.WriteLine("Executing " + step.Name);
                    step.WorkingSet = workingSet;
                    step.Execute();
                    Console.WriteLine("  Completed " + step.Name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }

            // Add all the items in the working set to the repository.
            Console.WriteLine("Registering all items in the working set with the repository");
            
            var client = Utility.GetClient();
            //client.RegisterItems(workingSet, new CommitOptions());
            //Console.WriteLine("  Done registering items");
        }
    }
}
