using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CloserDataPipeline
{
    public class PipelineDdiLists
    {
        public List<batch> batchList { get; protected set; }

        public PipelineDdiLists(string importPath)
        {
            batchList = new List<batch>();

            string[] lines = File.ReadAllLines(importPath);
            foreach (string line in lines)
            {
                string line2 = line.Trim();
                if (String.Equals(line2, ""))
                {
                    continue;
                }
                else if (line2[0] == '#')
                    //Console.WriteLine("Commented line: " + line);
                    continue;
                else
                {
                    //ddiFileList.Add(line2);
                    ParseLine(line2);
                }

            }
        }

        private void ParseLine(string line)
        {
            string[] parts = line.Split(new char[] { '\t' });
            if ((parts.Length == 2) && (parts[0] == "batch"))
            {
                string parsedBatchName = parts[1];
                batch newBatch = new batch(parsedBatchName);
                batchList.Add(newBatch);
            }
            else
            {
                //if the line starts with the name of a batch
                var relevantBatch = batchList.Find(x => string.Compare(x.batchName, parts[0], ignoreCase: true) == 0);
                if (relevantBatch != null)
                {
                    if ((parts.Length == 3) && (parts[1] == "CONCEPTS"))
                    {
                        relevantBatch.ddiFileList.Add(parts[2].Trim());
                    }
                    else if ((parts.Length == 3) && (parts[1] == "CADDIES"))
                    {
                        relevantBatch.ddiFileList.Add(parts[2].Trim());
                    }
                    else if ((parts.Length == 3) && (parts[1] == "TOPLEVEL"))
                    {
                        relevantBatch.ddiToplevelFileList.Add(parts[2].Trim());
                    }
                    else if ((parts.Length == 3) && (parts[1] == "VARIABLES"))
                    {
                        relevantBatch.ddiFileList.Add(parts[2].Trim());
                    }
                    else if ((parts.Length == 5) && (parts[1] == "MAPPING"))
                    {
                        string mappingFile = parts[2].Trim();
                        string ccScheme = parts[3].Trim();
                        string vScheme = parts[4].Trim();
                        ddiMappingFile mf = new ddiMappingFile(mappingFile, ccScheme, vScheme);
                        relevantBatch.ddiMappingFileList.Add(mf);
                    }
                    else if ((parts.Length == 4) && (parts[1] == "LINKING"))
                    {
                        string linkingFile = parts[2].Trim();
                        string vScheme = parts[3].Trim();
                        ddiLinkingFile lf = new ddiLinkingFile(linkingFile, vScheme);
                        relevantBatch.ddiLinkingFileList.Add(lf);
                    }
                    else if ((parts.Length == 4) && (parts[1] == "QLINK"))
                    {
                        string questionLinkingFile = parts[2].Trim();
                        string qcScheme = parts[3].Trim();
                        ddiQuestionLinkingFile lf = new ddiQuestionLinkingFile(questionLinkingFile, qcScheme);
                        relevantBatch.ddiQuestionLinkingFileList.Add(lf);
                    }
                    else if ((parts.Length == 4) && (parts[1] == "DERIVATION"))
                    {
                        string derivationFile = parts[2].Trim();
                        string vScheme = parts[3].Trim();
                        ddiDerivationFile df = new ddiDerivationFile(derivationFile, vScheme);
                        relevantBatch.ddiDerivationFileList.Add(df);
                    }
                    else
                    {
                        Trace.WriteLine("ERROR: Invalid batch line: " + line);
                    }
                }
                else
                {
                    //Trace.WriteLine("Irrelevant line: " + line);
                }
            }
        }
    }

    public class batch
    {
        public String batchName { get; protected set; }
        public List<string> ddiFileList { get; protected set; }
        public List<ddiMappingFile> ddiMappingFileList { get; protected set; }
        public List<ddiLinkingFile> ddiLinkingFileList { get; protected set; }
        public List<ddiQuestionLinkingFile> ddiQuestionLinkingFileList { get; protected set; }
        public List<ddiDerivationFile> ddiDerivationFileList { get; protected set; }
        public List<string> ddiToplevelFileList { get; protected set; }

        public batch(string bn)
        {
            batchName = bn;
            ddiFileList = new List<string>();
            ddiMappingFileList = new List<ddiMappingFile>();
            ddiLinkingFileList = new List<ddiLinkingFile>();
            ddiQuestionLinkingFileList = new List<ddiQuestionLinkingFile>();
            ddiDerivationFileList = new List<ddiDerivationFile>();
            ddiToplevelFileList = new List<string>();
        }
    }

    public class ddiMappingFile
    {
        public String mappingFileName { get; protected set; }
        public String ccsName { get; protected set; }
        public String vsName { get; protected set; }

        public ddiMappingFile(string mf, string ccs, string vs)
        {
            mappingFileName = mf;
            ccsName = ccs;
            vsName = vs;
        }
    }
   
    public class ddiLinkingFile
    {
        public String linkingFileName { get; protected set; }
        public String vsName { get; protected set; }

        public ddiLinkingFile(string lf, string vs)
        {
            linkingFileName = lf;
            vsName = vs;
        }
    }

    public class ddiQuestionLinkingFile
    {
         public String questionLinkingFileName { get; protected set; }
         public String qcsName { get; protected set; }

         public ddiQuestionLinkingFile(string qclf, string qcs)
         {
             questionLinkingFileName = qclf;
             qcsName = qcs;
         }
     }

    public class ddiDerivationFile
    {
        public String derivationFileName { get; protected set; }
        public String vsName { get; protected set; }

        public ddiDerivationFile(string df, string vs)
        {
            derivationFileName = df;
            vsName = vs;
        }
    }

 }
