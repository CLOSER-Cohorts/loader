using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloserDataPipeline.Steps
{
    public class DeriveVariables : IPipelineStep
    {
        string fileName;
        string vsName;

        public List<IVersionable> WorkingSet { get; set; }

        public string Name
        {
            get { return "Derive Variables - " + Path.GetFileName(fileName); }
        }

        public DeriveVariables(string fileName, string vsName)
        {
            this.fileName = fileName;
            this.vsName = vsName;
        }

        public void Execute()
        {
            // The file format is:
            //   [SourceVariableName] [Tab] [DerivedVariableName], many-to-many
            if (!System.IO.File.Exists(fileName))
            {
                Console.WriteLine("   missing file: " + fileName);
                return;
            }

            var variableScheme = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase:true) == 0).First();

            // Read each line.
            string[] lines = File.ReadAllLines(this.fileName);
            foreach (string line in lines)
            {
                // Break the line apart by the tab character.
                string[] parts = line.Split(new char[] { '\t' });
                if (parts.Length != 2)
                {
                    Console.WriteLine("      invalid line: " + line);
                    continue;
                }

                //swap column 0 and column 1 here if reqs change again
                string sourceVariable = parts[1].Trim();
                string derivedVariable = parts[0].Trim();

                if (sourceVariable == "0")
                {
                    Console.WriteLine("      [item] source variable is 0");
                    continue;
                }

                var matchingSources = variableScheme.Variables.Where(x => string.Compare(x.ItemName.Best, sourceVariable, ignoreCase: true) == 0);

                if (matchingSources.Count() == 0)
                {
                    Console.WriteLine("      [item] no variable named " + sourceVariable);
                    continue;
                }
                else if (matchingSources.Count() > 1)
                {
                    Console.WriteLine("      [item] multiple variables named " + sourceVariable);
                    continue;
                }

                var source = matchingSources.First();

                var matchingDVs = variableScheme.Variables.Where(x => string.Compare(x.ItemName.Best, derivedVariable, ignoreCase: true) == 0);

                if (matchingDVs.Count() == 0)
                {
                    Console.WriteLine("      [item] no variable named " + derivedVariable);
                    continue;
                }
                else if (matchingDVs.Count() > 1)
                {
                    Console.WriteLine("      [item] multiple variables named " + derivedVariable);
                    continue;
                }

                var dv = matchingDVs.First();

                // If we have correct matches, add the qsource as a source variable to the derivedvariable.
                dv.SourceVariables.Add(source);
            }
        }
    }
}
