using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace CloserDataPipeline.Steps
{
    public class LoadDdiFile : IPipelineStep
    {
        string fileName;

        public List<IVersionable> WorkingSet { get; set; }

        public string Name
        {
            get { return "Load DDI File - " + Path.GetFileName(fileName); }
        }

        public LoadDdiFile(string fileName)
        {
            this.fileName = fileName;
        }

        public void Execute()
        {
            // Load the DDI 3.2 file.
            if (!System.IO.File.Exists(fileName))
            {
                //throw new System.Exception("...Missing file: " + fileName);
                Trace.WriteLine("   missing file: " + fileName);
               return;
            }
            var validator = new DdiValidator(fileName, DdiFileFormat.Ddi32);
            bool isValid = validator.Validate();

            var doc = validator.ValidatedXDocument;

            var deserializer = new Ddi32Deserializer();
            var harmonized = deserializer.HarmonizeIdentifiers(doc, DdiFileFormat.Ddi32);
            
            var instance = deserializer.GetDdiInstance(doc.Root);

            
            // Get a list of all items contained in the DdiInstance.
            var gatherer = new ItemGathererVisitor();
            instance.Accept(gatherer);
            var allItems = gatherer.FoundItems;


            //test if there is a caddies-style question scheme (non-grid)
            //in which case add parameters
            var questionSchemes = allItems.OfType<QuestionScheme>().Where(x => string.Compare(x.ItemName.Best.Substring(x.ItemName.Best.Length - 5, 5), "_qs01") == 0);
            if (questionSchemes.Count() != 0)
            {
                var questionItems = questionSchemes.First().Questions;
                foreach (var qi in questionItems)
                {
                    var p = new Parameter();
                    p.ParameterType = InstrumentParameterType.Out;
                    p.Name.Add("en-GB", "p_" + qi.ItemName.Best);
                    qi.OutParameters.Add(p);
                }

                //I assume that there are the corresponding caddies question constructs
                //add an InParameter based on the first (the only) OutParameter of its QuestionItem,
                //an OutputParameter based on the construct name
                //and a binding between the two
                var questionConstructs = allItems.OfType<QuestionActivity>();
                foreach (var qc in questionConstructs)
                {
                    if (qc.Question != null)
                    {
                        var p = new Parameter();
                        p.ParameterType = InstrumentParameterType.In;
                        p.Name.Add("en-GB", qc.Question.OutParameters.First().Name.Best);
                        qc.InParameters.Add(p);
                        var p2 = new Parameter();
                        p2.ParameterType = InstrumentParameterType.Out;
                        p2.Name.Add("en-GB", "p_" + qc.ItemName.Best);
                        qc.OutParameters.Add(p2);
                        //var b = new Binding();
                        //b.SourceParameter = p;
                        //b.TargetParameter = p2;
                        //qc.Bindings.Add(b);
                    }
                    else if (qc.QuestionGrid != null)
                    {
                        continue;
                    }
                    else
                    {
                        Trace.WriteLine("   question construct with no source");
                    }
                }
            }                        
            // Add the items to the working set.
            WorkingSet.AddRange(allItems);
        }
    }
}
