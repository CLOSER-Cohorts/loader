using Algenta.Colectica.Commands.Import;
using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CloserDataPipeline.Steps
{
    public class LoadVariablesFromSpss : IPipelineStep
    {
        string fileName;

        public List<IVersionable> WorkingSet { get; set; }

        public string Name
        {
            get { return "Load Variables from SPSS - " + Path.GetFileName(fileName); }
        }

        public LoadVariablesFromSpss(string fileName)
        {
            this.fileName = fileName;
        }

        public void Execute()
        {
            // Use the SpssImporter to get a ResourcePackage with all metadata
            // contained in the SPSS file.
            var spssImporter = new SpssImporter();
            var resourcePackage = spssImporter.Import(fileName, "uk.cls");

            
            // Get a list of all items contained in the ResourcePackage.
            var gatherer = new ItemGathererVisitor();
            resourcePackage.Accept(gatherer);
            var allItems = gatherer.FoundItems;


            // Add the items to the repository.
            WorkingSet.AddRange(allItems);
        }
    }
}
