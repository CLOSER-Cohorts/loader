using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace CloserDataPipeline.Steps
{
    public class AttachExternalAids : IPipelineStep
    {
        string dirName;
        string ccsName;

        public List<IVersionable> WorkingSet { get; set; }

        public string Name
        {
            get { return "Attach External Aids - " + dirName; }
        }

        public AttachExternalAids(string dirName, string ccsName)
        {
            this.dirName = dirName;
            this.ccsName = ccsName;
        }

        public void Execute()
        {
            if (!System.IO.Directory.Exists(dirName))
            {
                Trace.WriteLine("   missing folder: " + dirName);
                return;
            }

            string[] fileEntries = System.IO.Directory.GetFiles(dirName);

            ControlConstructScheme ccs = new ControlConstructScheme();
            var client = Utility.GetClient();
            var facet = new SearchFacet();
            facet.ItemTypes.Add(DdiItemType.ControlConstructScheme);
            Trace.WriteLine("ccs: " + this.ccsName.Trim());
            SearchResponse response = client.Search(facet);
            Trace.WriteLine("results: " + response.ReturnedResults);
            foreach (var r in response.Results) 
            {
                Trace.WriteLine("        " + r.DisplayLabel);
                if (r.DisplayLabel == this.ccsName.Trim())
                {
                    ccs = client.GetItem(r.CompositeId, ChildReferenceProcessing.Populate) as ControlConstructScheme;
                }
            }
            if (response.ReturnedResults > 0)
            {
                ccs = client.GetItem(response.Results[0].CompositeId, ChildReferenceProcessing.Populate) as ControlConstructScheme;
            }

            var questions = ccs.ControlConstructs.OfType<QuestionActivity>();
            Trace.WriteLine("ccs count: " + questions.Count().ToString());

            foreach (string fileName in fileEntries)
            {
                string[] fileNamePieces = fileName.Split(new char[] { '\\' });
                string[] parts = fileNamePieces.Last().Split(new char[] { '.' });
                string qc = parts[0];
                string format = parts[1].ToLower();

                Trace.WriteLine("     qc: " + qc);
                Trace.WriteLine("     format: " + format);

                Trace.WriteLine("alpha" + qc);
                foreach (var q in questions)
                {
                    Trace.WriteLine(q.ItemName.Best);
                }
                var questionConstruct = questions.Single(x => string.Compare(x.ItemName.Best, qc, ignoreCase: true) == 0);
                Trace.WriteLine("here");
                var aid = new OtherMaterial();
                aid.MaterialType = "image";
                aid.MimeType = "image/png";
                aid.UrlReference = new System.Uri("/external_aids/" + dirName + "/" + fileName);
                string label = "";
                Trace.WriteLine("there");
                if (questionConstruct.Question != null) {
                    questionConstruct.Question.ExternalAids.Add(aid);
                    label = questionConstruct.Question.UserAttributes.First(x=>x.Key.ToString() == "extension:Label").Value.ToString();
                } else if (questionConstruct.QuestionGrid != null) {
                    questionConstruct.QuestionGrid.ExternalAids.Add(aid);
                    label = questionConstruct.QuestionGrid.UserAttributes.First(x=>x.Key.ToString() == "extension:Label").Value.ToString();
                }
                aid.DublinCoreMetadata.Title.SetStringForDefaultAudience("en-GB", label);
            }


            // Add the items to the repository.
            //WorkingSet.AddRange(allItems);
        }
    }
}
