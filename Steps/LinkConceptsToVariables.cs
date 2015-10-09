using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloserDataPipeline.Steps
{
    public class LinkConceptsToVariables : IPipelineStep
    {
        string fileName;
        string vsName;
        Dictionary<string, string> variablesConcepts;
        List<string> usedConcepts;

        public List<IVersionable> WorkingSet { get; set; }

        public string Name
        {
            get { return "Link Concepts to Variables"; }
        }

        public LinkConceptsToVariables(string fileName, string vsName)
        {
            this.fileName = fileName;
            this.vsName = vsName;
            this.variablesConcepts = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            this.usedConcepts = new List<string>();
        }

        //see execute2 below if we need to go back to a propagation model
        public void Execute()
        {
            if (!System.IO.File.Exists(fileName))
            {
                //throw new System.Exception("...Missing file: " + fileName);
                Console.WriteLine("...Missing file: " + fileName);
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
            //   [variableName] [Tab] [conceptIdx or 0]
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
                    Console.WriteLine("   invalid line: " + line);
                    continue;
                }

                string variableName = parts[0].Trim();
                string conceptIdx = parts[1].Trim();

                this.variablesConcepts.Add(variableName, conceptIdx);
            }

        }

 
        private void CreateVariableGroups()
         {
             //Get concepts that are used, add the implicit ones and except for "0" create variable groups from them in the relevant variable scheme and put into the working set
             this.usedConcepts = variablesConcepts.Values.Distinct().ToList();

             //add implicit level-1 groups that are parents of level-2 groups
             var implicits = new List<string>();
             foreach (var uc in this.usedConcepts)
             {
                 if (uc.Length == 1 + 2 + 2)
                 {
                     if (!usedConcepts.Contains(uc.Substring(0, 3)))
                     {
                         implicits.Add(uc.Substring(0,3));
                     }
                 }
             }
             this.usedConcepts.AddRange(implicits.Distinct());

            //get the concept scheme from the repository
            //I assume there is only one
            //if there is none, get concepts from the working set
            ConceptScheme vgConceptScheme = new ConceptScheme();
            var client = Utility.GetClient();
            var facet = new SearchFacet();
            facet.ItemTypes.Add(DdiItemType.ConceptScheme);
            SearchResponse response = client.Search(facet);
            bool fromRepo = false;
            if (response.ReturnedResults > 0)
            {
                fromRepo = true;
                vgConceptScheme = client.GetItem(response.Results[0].CompositeId, ChildReferenceProcessing.Populate) as ConceptScheme;
            }

             var variableScheme = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase: true) == 0).First();
             foreach (var uc in this.usedConcepts)
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
                     //Console.WriteLine("   " + vg.ItemName.Best);
                     variableScheme.VariableGroups.Add(vg);
                 }
             }
             WorkingSet.AddRange(variableScheme.VariableGroups);
             Console.WriteLine("  concept groups: " + variableScheme.VariableGroups.Count().ToString() + " for " + this.vsName);
         }


        private void BuildVariableGroupHierarchy()
         {
            //include level-2 groups into parent level-1 groups
            var variableScheme = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase: true) == 0).First();

            var level2VariableGroups = variableScheme.VariableGroups.Where(x => string.Compare(x.Concept.SubclassOf.First().ItemName.Best, "1") != 0);
            foreach (var vg2 in level2VariableGroups)
            {
                var parentConcept = vg2.Concept.SubclassOf.First();
                var parentGroup = variableScheme.VariableGroups.Where(x => x.Concept == parentConcept).First();
                parentGroup.ChildGroups.Add(vg2);
            }
         }

         private void AddVariablesToGroup()
        {
             //loop over the vars in the scheme rather than the vars from the linking file to catch the vars without concepts
            var variablesToAllocate = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase: true) == 0).First().Variables;
            foreach(var vta in variablesToAllocate)
            {
                if (variablesConcepts.Keys.Contains(vta.ItemName.Best))
                {
                    if (string.Compare(variablesConcepts[vta.ItemName.Best], "0") == 0)
                    {
                        Console.WriteLine("  variable with 0 topic: " + vta.ItemName.Best);
                    }
                    else
                    {
                        var variableGroup = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase: true) == 0).First().
                            VariableGroups.Where(x => string.Compare(x.Concept.ItemName.Best, variablesConcepts[vta.ItemName.Best]) == 0).First();
                        variableGroup.AddChild(vta);
                    }
                }
                else
                {
                    Console.WriteLine("  variable not in linking file: " + vta.ItemName.Best);
                }
            }
        }








        //ignore Execute2 which was based on propagation down the control construct tree, we have files with variables and concepts now
        //in progress
        public void Execute2()
        {
            // The file format is:
            //   [sequenceId] [Tab] [conceptName] [Tab] [conceptIdx or 0]
            //
            Dictionary<string, string> sequencesConcepts = new Dictionary<string, string>();

            // Read each line and put into dictionary.
            string[] lines = File.ReadAllLines(this.fileName);
            foreach (string line in lines)
            {
                // Break the line apart by the tab character.
                // The left side is the sequenceid. 
                // The middle is the concept name to ignore.
                // The right side is the concept index.
                string[] parts = line.Split(new char[] { '\t' });
                if (parts.Length != 3)
                {
                    Console.WriteLine("  invalid line: " + line);
                    continue;
                }

                string sequenceId = parts[0].Trim();
                string conceptIdx = parts[2].Trim();

                sequencesConcepts.Add(sequenceId, conceptIdx);
            }

            //Get concepts that are used and create variable groups from them in the relevant variable scheme
            List<string> usedConcepts = new List<string>();
            usedConcepts = sequencesConcepts.Values.Distinct().ToList();
            var variableScheme = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase:true) == 0).First();

            foreach (var uc in usedConcepts)
            {
                if (uc != "0")
                {
                    string conceptId = "concept1" + uc.PadLeft(2, '0');
                    Console.WriteLine("conceptId: " + conceptId);
                    VariableGroup vg = new VariableGroup();
                    vg.TypeOfGroup = "ConceptGroup";
                    var vgConcept = WorkingSet.OfType<Concept>().Where(x => string.Compare(x.UserIds.First().Identifier, conceptId, ignoreCase: true) == 0).First();
                    vg.Concept = vgConcept;
                    vg.ItemName.Add("en-GB", "Variable Group - " + vgConcept.ItemName.Best);
                    Console.WriteLine(vg.ItemName.Best);

                    variableScheme.VariableGroups.Add(vg);
                }
            }
            WorkingSet.AddRange(variableScheme.VariableGroups);
            Console.WriteLine("concept groups: " + variableScheme.VariableGroups.Count().ToString() + " for " + this.vsName);

            //find the sequences and tag with the concept by using a custom field
            var wsSequences = WorkingSet.OfType<CustomSequenceActivity>();
            //Console.WriteLine("sequences count: " + wsSequences.Count());
            foreach (var wsSequence in wsSequences)
            {
                if (sequencesConcepts.Keys.Contains(wsSequence.UserIds.First().Identifier))
                {
                    System.Console.WriteLine("sequences " + wsSequence.UserIds.First().Identifier + " " + wsSequence.Identifier + " " + sequencesConcepts[wsSequence.UserIds.First().Identifier]);
                    UserAttribute ua = new UserAttribute();
                    ua.Key = "GroupingConcept";
                    if (sequencesConcepts[wsSequence.UserIds.First().Identifier] != "0")
                    {
                        ua.Value = "concept1" + sequencesConcepts[wsSequence.UserIds.First().Identifier].PadLeft(2, '0');
                    }
                    else
                    {
                        ua.Value = "0";
                    }
                    wsSequence.UserAttributes.Add(ua);
                }
            }

            //for each sequence, traverse its descendant tree and propagate the concept tagging to direct question/variable descendants
            // for now, I assume that all sequences are listed in the linking file and I don't check the ccScheme
            foreach (var wsSequence in wsSequences)
            {
                if (sequencesConcepts.Keys.Contains(wsSequence.UserIds.First().Identifier))
                {
                    if (wsSequence.UserAttributes.Count() != 0)
                    {
                        Console.WriteLine("sequence: " + wsSequence.UserIds.First().Identifier + " " + wsSequence.UserAttributes.First().Value.Value);
                        if (string.Compare(wsSequence.UserAttributes.First().Value.Value, "0") != 0)
                        {
                            propagateTagging(wsSequence, "0");
                        }
                    }
                }
            }
        }

        public void propagateTagging(CustomSequenceActivity s, string parentTag)
        {

            foreach (var activity in s.Activities)
            {
                //Console.WriteLine(activity.GetType().Name);
                if (activity.GetType().Name == "StatementActivity")
                {
                    continue;
                }
                else if (activity.GetType().Name == "QuestionActivity")
                {
                    //get the variable group with the right concept
                    var variableGroup = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase: true) == 0).First().
                        VariableGroups.Where(x => string.Compare(x.Concept.UserIds.First().Identifier, s.UserAttributes.First().Value.Value) == 0).First();
                   //Console.WriteLine("variable group: " + variableGroup.Concept.UserIds.First().Identifier );
                   //get the variable from the question activity if it is a question item
                    QuestionActivity qc = (QuestionActivity)activity;
                    if (qc.Question != null)
                    {
                        Console.WriteLine("question item: " + qc.Question.ItemName.Best);
                        var variableToAdd = WorkingSet.OfType<VariableScheme>().Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase: true) == 0).First().
                            Variables.Where(x => x.SourceQuestions.Count() != 0).Where(x => string.Compare(x.SourceQuestions.First().ItemName.Best, qc.Question.ItemName.Best, ignoreCase: true) == 0).FirstOrDefault();
                        if (variableToAdd != null)
                        {
                            Console.WriteLine(" " + variableToAdd.ItemName.Best);
                            variableGroup.AddChild(variableToAdd);
                        }
                    }                    
                }
                else if (activity.GetType().Name == "CustomSequenceActivity")
                {
                    continue;
                    //Console.WriteLine("propagate");
                    //propagateTagging(activity);
                }
            }
        }
    }
}
