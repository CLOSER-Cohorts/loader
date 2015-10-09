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

namespace CloserDataPipeline.Steps
{
    public class LoadDdiToplevelFile : IPipelineStep
    {
        string fileName;

        public List<IVersionable> WorkingSet { get; set; }

        public string Name
        {
            get { return "Load DDI Toplevel File and mesh"; }
        }

        public LoadDdiToplevelFile(string fileName)
        {
            this.fileName = fileName;
        }

        public void Execute()
        {
            // Load the DDI 3.2 file.
            if (!System.IO.File.Exists(fileName))
            {
                //throw new System.Exception("...Missing file: " + fileName);
                Console.WriteLine("...Missing file: " + fileName);
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

            //mesh in the instrument references
            var dataCollections = allItems.OfType<DataCollection>();
            foreach (var dc in dataCollections)
            {
                //Console.WriteLine("data collection " + dc.ItemName.Best);
                var instrumentName = dc.ItemName.Best + "-in-000001";
                var instruments = WorkingSet.OfType<Instrument>().Where(x => string.Compare(x.UserIds[0].Identifier, instrumentName, ignoreCase: true) == 0);
                var ic = instruments.Count();
                if (ic == 1)
                    dc.AddChild(instruments.First());
                else if (ic > 1)
                   Console.WriteLine("More than one instrument found for " + instrumentName);
                else
                {
                    Console.WriteLine("No instrument found for " + instrumentName + ", to do: trying repo");
                    //var client = Utility.GetClient();
                    //var facet = new SearchFacet();
                    //facet.ItemTypes.Add(DdiItemType.Instrument);
                    //SearchResponse response = client.Search(facet);
                    //if (response.ReturnedResults > 1)
                    //{
                    //    var repoInstruments = client.GetItems(response.Results);
                    //}
                }
            }

            //mesh in variable set references
            //get all the study units (from toplevel, assumed) and go through all its data collections
            //find the logical products with a name matching the data collection name and attach them to the study unit
            //find physical data product and do the same, -I assume sdk physical product is ddi physical data product?-
            //find physicalInstance with alternate title matching data collection name and attach them to the study unit
            var studyUnits = allItems.OfType<StudyUnit>();
            foreach (var su in studyUnits)
            {
                foreach (var dc in su.DataCollections)
                {
                    var dcName =  dc.ItemName.Best;
                    
                    var matchingLogicalProducts = WorkingSet.OfType<LogicalProduct>().Where(x => string.Compare(x.ItemName.Best, dcName, ignoreCase: true) == 0);
                    var lpc = matchingLogicalProducts.Count();
                    if (lpc == 1)
                        su.AddChild(matchingLogicalProducts.First());
                    else if (lpc > 1)
                        Console.WriteLine("More than one logical product found for " + dcName);
                    else
                        Console.WriteLine("No logical product found for " + dcName);

                    var matchingPhysicalProducts = WorkingSet.OfType<PhysicalProduct>().Where(x => string.Compare(x.ItemName.Best, dcName, ignoreCase: true) == 0);
                    var pdpc = matchingPhysicalProducts.Count();
                    if (pdpc == 1)
                        su.AddChild(matchingPhysicalProducts.First());
                    else if (pdpc > 1)
                        Console.WriteLine("More than one physical data product found for " + dcName);
                    else
                        Console.WriteLine("No physical data product found for " + dcName);

                    var matchingPhysicalInstances = WorkingSet.OfType<PhysicalInstance>().Where(x => string.Compare(x.DublinCoreMetadata.AlternateTitle.Best, dcName, ignoreCase: true) == 0);
                    var pic = matchingPhysicalInstances.Count();
                    if (pic == 1)
                        su.AddChild(matchingPhysicalInstances.First());
                    else if (pic > 1)
                        Console.WriteLine("More than one physical instance found for " + dcName);
                    else
                        Console.WriteLine("No physical instance found for " + dcName);

                    var matchingResourcePackages = WorkingSet.OfType<ResourcePackage>().Where(x => string.Compare(x.DublinCoreMetadata.AlternateTitle.Best, dcName, ignoreCase: true) == 0);
                    var rpc = matchingResourcePackages.Count();
                    if (rpc == 1)
                        su.AddChild(matchingResourcePackages.First());
                    else if (rpc > 1)
                        Console.WriteLine("More than one resource package found for " + dcName);
                    else
                        Console.WriteLine("No resource package found for " + dcName);
                }
            }

            // Add the items to the working set.
            WorkingSet.AddRange(allItems);
        }
    }
}
