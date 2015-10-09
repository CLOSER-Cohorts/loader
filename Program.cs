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
            Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(@"d:\development\claude\repo_ingest\imports\loader.log", "myListener"));
            Trace.WriteLine("LOADER START: " + DateTime.Now.ToString("s"));

            MultilingualString.CurrentCulture = "en-GB";
            VersionableBase.DefaultAgencyId = "uk.cls";

            var runner = new PipelineRunner();

            //files of lists of files to be ingested
            string listPath = @"d:\development\claude\repo_ingest\imports\ddifiles.txt";

            //the directory where these files are 
            string basePath = @"";      //for when the files are in multiple places

            //Console.WriteLine("Start loader...");

            var ddiLists = new PipelineDdiLists(listPath);
            //ddiLists.AddMappingList(mappingListPath);
            //ddiLists.AddLinkingList(linkingListPath);
            //ddiLists.AddQuestionLinkingList(questionLinkingListPath);
            //ddiLists.AddDerivationList(derivationListPath);

            //put all the ddi32 files into pipeline steps
            foreach (string fileName in ddiLists.ddiFileList)
            {
                System.Console.WriteLine("ddi file to load: " + fileName);
                runner.Steps.Add(new LoadDdiFile(Path.Combine(basePath, fileName)));
            }
            System.Console.WriteLine("total: " + ddiLists.ddiFileList.Count());

            foreach (string fileName in ddiLists.ddiToplevelFileList)
            {
                System.Console.WriteLine("ddi toplevel file to load and mesh: " + fileName);
                runner.Steps.Add(new LoadDdiToplevelFile(Path.Combine(basePath, fileName)));
            }
            System.Console.WriteLine("total: " + ddiLists.ddiToplevelFileList.Count());

            //add the source question item to the variables into pipeline steps
            foreach (ddiMappingFile mf in ddiLists.ddiMappingFileList)
            {
                System.Console.WriteLine("mapping file to load: " + mf.mappingFileName);
                runner.Steps.Add(new MapVariablesToQuestions(Path.Combine(basePath, mf.mappingFileName), mf.ccsName, mf.vsName));
            }
            System.Console.WriteLine("total: " + ddiLists.ddiMappingFileList.Count());

            //for each linking file, add VariableGroups
            foreach (ddiLinkingFile lf in ddiLists.ddiLinkingFileList)
            {
                System.Console.WriteLine("linking file to load: " + lf.linkingFileName);
                runner.Steps.Add(new LinkConceptsToVariables(Path.Combine(basePath, lf.linkingFileName), lf.vsName));
            }
            System.Console.WriteLine("total: " + ddiLists.ddiLinkingFileList.Count());

            //for each question linking file, add QuestionGroups
            foreach (ddiQuestionLinkingFile qclf in ddiLists.ddiQuestionLinkingFileList)
            {
                System.Console.WriteLine("question linking file to load: " + qclf.questionLinkingFileName);
                runner.Steps.Add(new LinkConceptsToQuestions(Path.Combine(basePath, qclf.questionLinkingFileName), qclf.qcsName));
            }
            System.Console.WriteLine("total: " + ddiLists.ddiQuestionLinkingFileList.Count());

            //for each variable derivation file, add Derivations
            foreach (ddiDerivationFile df in ddiLists.ddiDerivationFileList)
            {
                System.Console.WriteLine("variable derivation file to load: " + df.derivationFileName);
                runner.Steps.Add(new DeriveVariables(Path.Combine(basePath, df.derivationFileName), df.vsName));
            }
            System.Console.WriteLine("total: " + ddiLists.ddiDerivationFileList.Count());


            //commit to the repository
            runner.Run();

            Trace.WriteLine("LOADER END: " + DateTime.Now.ToString("s"));
            //Trace.WriteLine("");
            Trace.Flush();

            // Remove the ReadLine() call to allow the console application to exit 
            // without user input.
            Console.WriteLine("Finished. Press enter to exit");
            Console.ReadLine();
        }
    }
}
