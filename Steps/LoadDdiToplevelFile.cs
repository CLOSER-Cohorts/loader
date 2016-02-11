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
    public class LoadDdiToplevelFile : IPipelineStep
    {
        string fileName;

        public List<IVersionable> WorkingSet { get; set; }

        public string Name
        {
            get { return "Load DDI Toplevel File and mesh - " + Path.GetFileName(fileName); }
        }

        public LoadDdiToplevelFile(string fileName)
        {
            this.fileName = fileName;
        }

        public void Execute()
        {
            // Load the DDI 3.2 toplevel file.
            if (!System.IO.File.Exists(fileName))
            {
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

            //mesh in the instrument references
            //nb: ItemName seems to be the empty string rather than null if it does not exist in the ddi file
            var dataCollections = allItems.OfType<DataCollection>().Where(x => string.Compare(x.ItemName.Best, "") != 0);
            foreach (var dc in dataCollections)
            {
                var instrumentName = dc.ItemName.Best + "-in-000001";
                var instruments = WorkingSet.OfType<Instrument>().Where(x => string.Compare(x.UserIds[0].Identifier, instrumentName, ignoreCase: true) == 0);
                var ic = instruments.Count();
                if (ic == 1)
                    dc.AddChild(instruments.First());
                else if (ic > 1)
                   Trace.WriteLine("   more than one instrument found for " + instrumentName);
                else
                {
                    Trace.WriteLine("   no instrument found for " + instrumentName);
                    //to do: try repo
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
            //I assume that there may be more than one corresponding dataset (i.e. its log prod, phys prod...) with same base name but with a hyphenated suffix
            //get all the study units (from toplevel, assumed) and go through all its data collections
            //find the logical products with a base name matching the data collection name and attach them to the study unit
            //find physical data product and do the same, -I assume sdk physical product is ddi physical data product?-
            //find physicalInstance with base alternate title matching data collection name and attach them to the study unit
            var studyUnits = allItems.OfType<StudyUnit>();
            foreach (var su in studyUnits)
            {
                foreach (var dc in su.DataCollections.Where(x => string.Compare(x.ItemName.Best, "") != 0))
                {
                    var dcName =  dc.ItemName.Best;
                    
                    var matchingLogicalProducts = WorkingSet.OfType<LogicalProduct>().Where(x => string.Compare(x.ItemName.Best.Split('-')[0], dcName, ignoreCase: true) == 0);
                    var lpc = matchingLogicalProducts.Count();
                    //if (lpc == 1)
                    //    su.AddChild(matchingLogicalProducts.First());
                    //else if (lpc > 1)
                    //    Trace.WriteLine("   more than one logical product found for " + dcName);
                    if (lpc > 0)
                        foreach (var lp in matchingLogicalProducts)
                            su.AddChild(lp);
                    else
                        Trace.WriteLine("   no logical product found for " + dcName);

                    var matchingPhysicalProducts = WorkingSet.OfType<PhysicalProduct>().Where(x => string.Compare(x.ItemName.Best.Split('-')[0], dcName, ignoreCase: true) == 0);
                    var pdpc = matchingPhysicalProducts.Count();
                    //if (pdpc == 1)
                    //    su.AddChild(matchingPhysicalProducts.First());
                    //else if (pdpc > 1)
                    //    Trace.WriteLine("   more than one physical data product found for " + dcName);
                    if (pdpc > 0)
                        foreach (var pp in matchingPhysicalProducts)
                            su.AddChild(pp);
                    else
                        Trace.WriteLine("   no physical data product found for " + dcName);

                    var matchingPhysicalInstances = WorkingSet.OfType<PhysicalInstance>().Where(x => string.Compare(x.DublinCoreMetadata.AlternateTitle.Best.Split('-')[0], dcName, ignoreCase: true) == 0);
                    var pic = matchingPhysicalInstances.Count();
                    //if (pic == 1)
                    //    su.AddChild(matchingPhysicalInstances.First());
                    //else if (pic > 1)
                    //    Trace.WriteLine("   more than one physical instance found for " + dcName);
                    if (pic > 0)
                        foreach (var pi in matchingPhysicalInstances)
                            su.AddChild(pi);
                    else
                        Trace.WriteLine("   no physical instance found for " + dcName);

                    var matchingResourcePackages = WorkingSet.OfType<ResourcePackage>().Where(x => string.Compare(x.DublinCoreMetadata.AlternateTitle.Best.Split('-')[0], dcName, ignoreCase: true) == 0);
                    var rpc = matchingResourcePackages.Count();
                    //if (rpc == 1)
                    //    su.AddChild(matchingResourcePackages.First());
                    //else if (rpc > 1)
                    //    Trace.WriteLine("   more than one resource package found for " + dcName);
                    if (rpc > 0)
                        foreach (var rp in matchingResourcePackages)
                            su.AddChild(rp);
                    else
                        Trace.WriteLine("   no resource package found for " + dcName);
                }
            }

            // Add the items to the working set.
            WorkingSet.AddRange(allItems);
        }
    }
}
