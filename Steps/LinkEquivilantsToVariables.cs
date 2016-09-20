using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CloserDataPipeline.Steps
{
    public class LinkEquivalentsToVariables : IPipelineStep
    {
        string fileName;
        Dictionary<string, string []> variablesEquivalents;
        List<string> usedEquivalents;

        public List<IVersionable> WorkingSet { get; set; }

        public string Name
        {
            get { return "Link Equivilant Variables - " + Path.GetFileName(fileName); }
        }

        public LinkEquivalentsToVariables(string fileName)
        {
            this.fileName = fileName;
            this.variablesEquivalents = new Dictionary<string,string []>(StringComparer.OrdinalIgnoreCase);
            this.usedEquivalents = new List<string>();
        }

        //see execute2 below if we need to go back to a propagation model
        public void Execute()
        {
            if (!System.IO.File.Exists(fileName))
            {
                //throw new System.Exception("...Missing file: " + fileName);
                Trace.WriteLine("   missing file: " + fileName);
                return;
            }

            ReadFileIntoDictionary();
            CreateVariableGroups();
            BuildVariableGroupHierarchy();
            AddVariablesToGroup();
        }

         private void ReadFileIntoDictionary()
        {
            // The file format is:
            //   [variableName] [Tab] [equivIdx or 0]
            // Read each line and put into dictionary.
            string[] lines = File.ReadAllLines(this.fileName);
            foreach (string line in lines)
            {
                // Break the line apart by the tab character.
                // The left side is the variableName. 
                // The right is the concept index.
                string[] parts = line.Split(new char[] { '\t' });
                if (parts.Length != 2)
                {
                    Trace.WriteLine("      invalid line: " + line);
                    continue;
                }

                string scope = parts[0].Trim();
                string varname = parts[1].Trim();

                //this.variablesEquivalents.Add(scope, new string[] { varname });
            }
        }

        private void CreateVariableGroups()
         {
             //Get concepts that are used, add the implicit ones and except for "0" create variable groups from them in the relevant variable scheme and put into the working set
             //this.usedEquivalents = variablesEquivalents.Values.Distinct().ToList();

            //get the concept scheme from the repository
            //I assume there is only one
            //if there is none, get concepts from the working set
            ConceptScheme vgConceptScheme = new ConceptScheme();
            var client = Utility.GetClient();
            var facet = new SearchFacet();
            facet.ItemTypes.Add(DdiItemType.ConceptScheme);
            facet.SearchTerms.Add("CLOSER Equivalents");
            SearchResponse response = client.Search(facet);
            bool fromRepo = false;
            if (response.ReturnedResults > 0)
            {
                fromRepo = true;
                vgConceptScheme = client.GetItem(response.Results[0].CompositeId, ChildReferenceProcessing.Populate) as ConceptScheme;
            }

             //var variableScheme = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase: true) == 0).First();
             foreach (var uc in this.usedEquivalents)
             {
                 if (uc != "0")
                 {
                     VariableGroup vg = new VariableGroup();
                     vg.TypeOfGroup = "ConceptGroup";
                     Concept vgConcept = new Concept();
                     if (fromRepo)
                     {
                         vgConcept = vgConceptScheme.Concepts.Where(x => string.Compare(x.ItemName.Best, uc, ignoreCase: true) == 0).First();
                     }
                     else    //from working set
                     {
                         vgConcept = WorkingSet.OfType<Concept>().Where(x => string.Compare(x.ItemName.Best, uc, ignoreCase: true) == 0).First();
                     }

                     vg.Concept = vgConcept;
                     vg.ItemName.Add("en-GB", "Variable Group - " + vgConcept.Label.Best);
                     //Trace.WriteLine("   " + vg.ItemName.Best);
                     //variableScheme.VariableGroups.Add(vg);
                 }
             }
             //WorkingSet.AddRange(variableScheme.VariableGroups);
             //Trace.WriteLine("  concept groups: " + variableScheme.VariableGroups.Count().ToString() + " for " + this.vsName);
         }

        private void BuildVariableGroupHierarchy()
         {
            //include level-2 groups into parent level-1 groups
            /*var variableScheme = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase: true) == 0).First();

            //var level2VariableGroups = variableScheme.VariableGroups.Where(x => string.Compare(x.Concept.SubclassOf.First().ItemName.Best, "1") != 0);
            var level2VariableGroups = variableScheme.VariableGroups.Where(x => x.Concept.SubclassOf.Count() > 0);
            foreach (var vg2 in level2VariableGroups)
            {
                var parentConcept = vg2.Concept.SubclassOf.First();
                var parentGroup = variableScheme.VariableGroups.Where(x => x.Concept == parentConcept).First();
                parentGroup.ChildGroups.Add(vg2);
            }*/
         }

         private void AddVariablesToGroup()
        {
             //loop over the vars in the scheme rather than the vars from the linking file to catch the vars without concepts
            /*var variablesToAllocate = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase: true) == 0).First().Variables;
            foreach(var vta in variablesToAllocate)
            {
                if (variablesEquivalents.Keys.Contains(vta.ItemName.Best))
                {
                    /*if (string.Compare(variablesEquivalents[vta.ItemName.Best], "0") == 0)
                    {
                        Trace.WriteLine("     variable with 0 topic: " + vta.ItemName.Best);
                    }
                    else
                    {
                        var variableGroup = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase: true) == 0).First().
                            VariableGroups.Where(x => string.Compare(x.Concept.ItemName.Best, variablesEquivalents[vta.ItemName.Best]) == 0).First();
                        variableGroup.AddChild(vta);
                    }
                }
                else
                {
                    Trace.WriteLine("     variable not in linking file: " + vta.ItemName.Best);
                }
            }*/
        }
     }
}
