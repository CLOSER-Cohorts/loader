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
    //this class groups question constructs by topic
    public class LinkConceptsToQuestions : IPipelineStep
    {
        string fileName;
        string qcsName;
        Dictionary<string, string> questionsConcepts;
        List<string> usedConcepts;

        public List<IVersionable> WorkingSet { get; set; }

        public string Name
        {
            get { return "Link Concepts to Questions Constructs"; }
        }

        public LinkConceptsToQuestions(string fileName, string qcsName)
        {
            this.fileName = fileName;
            this.qcsName = qcsName;
            this.questionsConcepts = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            this.usedConcepts = new List<string>();
        }

        public void Execute()
        {
            if (!System.IO.File.Exists(fileName))
            {
                Console.WriteLine("...Missing file: " + fileName);
                return;
            }

            ReadFileIntoDictionary();
            CreateQuestionGroups();
            BuildQuestionGroupHierarchy();
            AddQuestionsToGroup();
        }

        private void ReadFileIntoDictionary()
        {
            // The file format is:
            //   [questionConstructName] [Tab] [conceptIdx or 0]
            // Read each line and put into dictionary.
            string[] lines = File.ReadAllLines(this.fileName);
            foreach (string line in lines)
            {
                // Break the line apart by the tab character.
                // The left side is the questionName. 
                // The right is the concept index.
                string[] parts = line.Split(new char[] { '\t' });
                if (parts.Length != 2)
                {
                    Console.WriteLine("   invalid line: " + line);
                    continue;
                }

                string questionName = parts[0].Trim();
                string conceptIdx = parts[1].Trim();

                this.questionsConcepts.Add(questionName, conceptIdx);
            }

        }

        private void CreateQuestionGroups()
        {
            //Get concepts that are used, add the implicit ones and except for "0" create question groups from them in the relevant question scheme and put into the working set
            this.usedConcepts = questionsConcepts.Values.Distinct().ToList();

            //add implicit level-1 groups that are parents of level-2 groups
            var implicits = new List<string>();
            foreach (var uc in this.usedConcepts)
            {
                if (uc.Length == 1 + 2 + 2)
                {
                    if (!usedConcepts.Contains(uc.Substring(0, 3)))
                    {
                        implicits.Add(uc.Substring(0, 3));
                    }
                }
            }
            this.usedConcepts.AddRange(implicits.Distinct());

            //get the concept scheme from the repository
            //I assume there is only one
            //if there is none, get concepts from the working set
            ConceptScheme qcgConceptScheme = new ConceptScheme();
            var client = Utility.GetClient();
            var facet = new SearchFacet();
            facet.ItemTypes.Add(DdiItemType.ConceptScheme);
            SearchResponse response = client.Search(facet);
            bool fromRepo = false;
            if (response.ReturnedResults > 0)
            {
                fromRepo = true;
                qcgConceptScheme = client.GetItem(response.Results[0].CompositeId, ChildReferenceProcessing.Populate) as ConceptScheme;
            }

            var controlConstructScheme = WorkingSet.OfType<ControlConstructScheme>().Where(x => string.Compare(x.ItemName.Best, this.qcsName, ignoreCase: true) == 0).First();
            foreach (var uc in this.usedConcepts)
            {
                if (uc != "0")
                {
                    ControlConstructGroup qcg = new ControlConstructGroup();
                    qcg.TypeOfGroup = "ConceptGroup";
                    Concept qcgConcept = new Concept();
                    if (fromRepo)
                    {
                        qcgConcept = qcgConceptScheme.Concepts.Where(x => string.Compare(x.ItemName.Best, uc, ignoreCase: true) == 0).First();
                    }
                    else    //from working set
                    {
                        qcgConcept = WorkingSet.OfType<Concept>().Where(x => string.Compare(x.ItemName.Best, uc, ignoreCase: true) == 0).First();
                    }
                    qcg.Concept = qcgConcept;
                    qcg.ItemName.Add("en-GB", "Question Construct Group - " + qcgConcept.Label.Best);
                    //Console.WriteLine("   " + qcg.ItemName.Best);
                    controlConstructScheme.ControlConstructGroups.Add(qcg);
                }
            }
            WorkingSet.AddRange(controlConstructScheme.ControlConstructGroups);
            Console.WriteLine("  question construct groups: " + controlConstructScheme.ControlConstructGroups.Count().ToString() + " for " + this.qcsName);
        }

        private void BuildQuestionGroupHierarchy()
        {
            //include level-2 groups into parent level-1 groups
            var controlConstructScheme = WorkingSet.OfType<ControlConstructScheme>().Where(x => string.Compare(x.ItemName.Best, this.qcsName, ignoreCase: true) == 0).First();

            var level2QuestionGroups = controlConstructScheme.ControlConstructGroups.Where(x => string.Compare(x.Concept.SubclassOf.First().ItemName.Best, "1") != 0);
            foreach (var qcg2 in level2QuestionGroups)
            {
                var parentConcept = qcg2.Concept.SubclassOf.First();
                var parentGroup = controlConstructScheme.ControlConstructGroups.Where(x => x.Concept == parentConcept).First();
                parentGroup.ChildGroups.Add(qcg2);
            }
        }

        private void AddQuestionsToGroup()
        {
            //loop over the question constructs in the scheme rather than the questions from the linking file to catch the questions without concepts
            var questionsToAllocate = WorkingSet.OfType<ControlConstructScheme>().Where(x => string.Compare(x.ItemName.Best, this.qcsName, ignoreCase: true) == 0).First().ControlConstructs.OfType<QuestionActivity>();
            foreach (var qta in questionsToAllocate)
            {
                if (questionsConcepts.Keys.Contains(qta.ItemName.Best))
                {
                    if (string.Compare(questionsConcepts[qta.ItemName.Best], "0") == 0)
                    {
                        Console.WriteLine("  question with 0 topic: " + qta.ItemName.Best);
                    }
                    else
                    {
                        var questionGroup = WorkingSet.OfType<ControlConstructScheme>().Where(x => string.Compare(x.ItemName.Best, this.qcsName, ignoreCase: true) == 0).First().
                            ControlConstructGroups.Where(x => string.Compare(x.Concept.ItemName.Best, questionsConcepts[qta.ItemName.Best]) == 0).First();
                        questionGroup.AddChild(qta);
                    }
                }
                else
                {
                    Console.WriteLine("  question not in linking file: " + qta.ItemName.Best);
                }
            }
        }

    }
}
