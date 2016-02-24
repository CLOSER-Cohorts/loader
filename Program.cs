using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using CloserDataPipeline.Steps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CloserDataPipeline
{
    class Program
    {
        static void Main(string[] args)
        {
            //Trace.Listeners.Clear();      //don't remove IDE output window
            Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
            Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(@"d:\CLOSER\development\claude\repo_ingest\imports\loader.log", "myListener"));
            Trace.WriteLine("LOADER START: " + DateTime.Now.ToString("s"));
            Trace.WriteLine("Ignore the log4net messages");    //has disappeared after adding System.Diagnostics to PipelineDdiLists.cs

            MultilingualString.CurrentCulture = "en-GB";
            VersionableBase.DefaultAgencyId = "uk.cls";

            //file of lists of files to be ingested
            string listPath = @"d:\closer\development\claude\repo_ingest\imports\ddifiles.txt";

            //read the batches and lists of files
            var ddiLists = new PipelineDdiLists(listPath);

            //loop over the batches
            foreach(var batch in ddiLists.batchList)
            {
                Trace.WriteLine("***START Batch: " + batch.batchName + "***");
                Trace.WriteLine("Creating steps...");
                //a new runner for each batch
                var runner = new PipelineRunner();

                //put all the ddi32 files (concepts, caddies and variables) into pipeline steps
                foreach (string fileName in batch.ddiFileList)
                {
                    Trace.WriteLine("  " + fileName);
                    runner.Steps.Add(new LoadDdiFile(Path.Combine(ddiLists.basePath, fileName)));
                }
                Trace.WriteLine(" total ddi files to load: " + batch.ddiFileList.Count());

                //put all the toplevel files into pipeline steps
                foreach (string fileName in batch.ddiToplevelFileList)
                {
                    Trace.WriteLine("  " + fileName);
                    runner.Steps.Add(new LoadDdiToplevelFile(Path.Combine(ddiLists.basePath, fileName)));
                }
                Trace.WriteLine(" total toplevel file to load and mesh: " + batch.ddiToplevelFileList.Count());

                //put adding the source question item to the variables into pipeline steps
                foreach (ddiMappingFile mf in batch.ddiMappingFileList)
                {
                    Trace.WriteLine("  " + mf.mappingFileName);
                    runner.Steps.Add(new MapVariablesToQuestions(Path.Combine(ddiLists.basePath, mf.mappingFileName), mf.ccsName, mf.ccsNameList, mf.vsName));
                }
                Trace.WriteLine(" total mapping file to load: " + batch.ddiMappingFileList.Count());

                //put adding VariableGroups for each linking file into pipeline steps 
                foreach (ddiLinkingFile lf in batch.ddiLinkingFileList)
                {
                    Trace.WriteLine("  " + lf.linkingFileName);
                    runner.Steps.Add(new LinkConceptsToVariables(Path.Combine(ddiLists.basePath, lf.linkingFileName), lf.vsName));
                }
                Trace.WriteLine(" total linking file to load: " + batch.ddiLinkingFileList.Count());

                //put adding QuestionGroups for each question linking file into pipeline steps
                foreach (ddiQuestionLinkingFile qclf in batch.ddiQuestionLinkingFileList)
                {
                    Trace.WriteLine("  " + qclf.questionLinkingFileName);
                    runner.Steps.Add(new LinkConceptsToQuestions(Path.Combine(ddiLists.basePath, qclf.questionLinkingFileName), qclf.qcsName));
                }
                Trace.WriteLine(" total question linking file to load: " + batch.ddiQuestionLinkingFileList.Count());

                //put adding Derivations for each variable derivation file into pipeline steps
                foreach (ddiDerivationFile df in batch.ddiDerivationFileList)
                {
                    Trace.WriteLine("  " + df.derivationFileName);
                    runner.Steps.Add(new DeriveVariables(Path.Combine(ddiLists.basePath, df.derivationFileName), df.vsName));
                }
                Trace.WriteLine(" total variable derivation file to load: " + batch.ddiDerivationFileList.Count());


                //digest the batch and commit to the repository
                runner.Run();
            }

            Trace.WriteLine("LOADER END: " + DateTime.Now.ToString("s"));
            Trace.WriteLine("");
            Trace.Flush();

            // Remove the ReadLine() call to allow the console application to exit 
            // without user input.
            Console.WriteLine("Finished. Press enter to exit");
            Console.ReadLine();
        }
    }
}
