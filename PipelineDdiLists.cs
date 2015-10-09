using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloserDataPipeline
{
    public class PipelineDdiLists
    {
        public string batch { get; protected set; }
        public List<string> ddiFileList { get; protected set; }
        public List<ddiMappingFile> ddiMappingFileList { get; protected set; }
        public List<ddiLinkingFile> ddiLinkingFileList { get; protected set; }
        public List<ddiQuestionLinkingFile> ddiQuestionLinkingFileList { get; protected set; }
        public List<ddiDerivationFile> ddiDerivationFileList { get; protected set; }
        public List<string> ddiToplevelFileList { get; protected set; }

        public PipelineDdiLists(string importPath)
        {
            ddiFileList = new List<string>();
            ddiMappingFileList = new List<ddiMappingFile>();
            ddiLinkingFileList = new List<ddiLinkingFile>();
            ddiQuestionLinkingFileList = new List<ddiQuestionLinkingFile>();
            ddiDerivationFileList = new List<ddiDerivationFile>();
            ddiToplevelFileList = new List<string>();
            batch = "";

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
                batch = parts[1];
            }
            else if (parts[0] == batch)
            { 
                if ((parts.Length == 3) && (parts[1] == "CONCEPTS"))
                {
                    ddiFileList.Add(parts[2].Trim());
                }
                else if ((parts.Length == 3) && (parts[1] == "CADDIES"))
                {
                    ddiFileList.Add(parts[2].Trim());
                }
                else if ((parts.Length == 3) && (parts[1] == "TOPLEVEL"))
                {
                    ddiToplevelFileList.Add(parts[2].Trim());
                }
                else if ((parts.Length == 3) && (parts[1] == "VARIABLES"))
                {
                    ddiFileList.Add(parts[2].Trim());
                }
                else if ((parts.Length == 5) && (parts[1] == "MAPPING"))
                {
                    string mappingFile = parts[2].Trim();
                    string ccScheme = parts[3].Trim();
                    string vScheme = parts[4].Trim();
                    ddiMappingFile mf = new ddiMappingFile(mappingFile, ccScheme, vScheme);
                    ddiMappingFileList.Add(mf);
                }
                else if ((parts.Length == 4) && (parts[1] == "LINKING"))
                {
                    string linkingFile = parts[2].Trim();
                    string vScheme = parts[3].Trim();
                    ddiLinkingFile lf = new ddiLinkingFile(linkingFile, vScheme);
                    ddiLinkingFileList.Add(lf);
                }
                else if ((parts.Length == 4) && (parts[1] == "QLINK"))
                {
                    string questionLinkingFile = parts[2].Trim();
                    string qcScheme = parts[3].Trim();
                    ddiQuestionLinkingFile lf = new ddiQuestionLinkingFile(questionLinkingFile, qcScheme);
                    ddiQuestionLinkingFileList.Add(lf);
                }
                else if ((parts.Length == 4) && (parts[1] == "DERIVATION"))
                {
                    string derivationFile = parts[2].Trim();
                    string vScheme = parts[3].Trim();
                    ddiDerivationFile df = new ddiDerivationFile(derivationFile, vScheme);
                    ddiDerivationFileList.Add(df);
                }
                else
                {
                    Console.WriteLine("Invalid batch line: " + line);
                }

            }
            else
            {
                //Console.WriteLine("Irrelevant line: " + line);
            }
        }

        //public void AddMappingList(string importMappingPath)
        //{
        //    //ddiMappingFileList = new List<string>();
        //    ddiMappingFileList = new List<ddiMappingFile>();
        //    string[] lines = File.ReadAllLines(importMappingPath);
        //    foreach (string line in lines)
        //    {
        //        string line2 = line.Trim();
        //        if (String.Equals(line2, ""))
        //        {
        //            continue;
        //        }
        //        else if (line2[0] == '#')
        //            continue;
        //        else
        //        {
        //            string[] parts = line2.Split(new char[] { '\t' });
        //            if (parts.Length != 3)
        //            {
        //                Console.WriteLine("Invalid line: " + line2);
        //                continue;
        //            }

        //            string mappingFile = parts[0].Trim();
        //            string ccScheme = parts[1].Trim();
        //            string vScheme = parts[2].Trim();
        //            ddiMappingFile mf = new ddiMappingFile(mappingFile, ccScheme, vScheme);
        //            //ddiMappingFileList.Add(mappingFile);
        //            ddiMappingFileList.Add(mf);
        //        }

        //    }
        //}
   
        //public void AddLinkingList(string importLinkingPath)
        //{
        //    ddiLinkingFileList = new List<ddiLinkingFile>();
        //    string[] lines = File.ReadAllLines(importLinkingPath);
        //    foreach (string line in lines)
        //    {
        //        string line2 = line.Trim();
        //        if (String.Equals(line2, ""))
        //        {
        //            continue;
        //        }
        //        else if (line2[0] == '#')
        //        {
        //            continue;
        //        }
        //        else
        //        {
        //            string[] parts = line2.Split(new char[] { '\t' });
        //            if (parts.Length != 2)
        //            {
        //                Console.WriteLine("Invalid line: " + line2);
        //                continue;
        //            }

        //            string linkingFile = parts[0].Trim();
        //            string vScheme = parts[1].Trim();
        //            ddiLinkingFile lf = new ddiLinkingFile(linkingFile, vScheme);
        //            ddiLinkingFileList.Add(lf);
        //        }

        //    }
        //}

        //public void AddQuestionLinkingList(string importQuestionLinkingPath)
        //{
        //    ddiQuestionLinkingFileList = new List<ddiQuestionLinkingFile>();
        //    string[] lines = File.ReadAllLines(importQuestionLinkingPath);
        //    foreach (string line in lines)
        //    {
        //        string line2 = line.Trim();
        //        if (String.Equals(line2, ""))
        //        {
        //            continue;
        //        }
        //        else if (line2[0] == '#')
        //        {
        //            continue;
        //        }
        //        else
        //        {
        //            string[] parts = line2.Split(new char[] { '\t' });
        //            if (parts.Length != 2)
        //            {
        //                Console.WriteLine("Invalid line: " + line2);
        //                continue;
        //            }

        //            string questionLinkingFile = parts[0].Trim();
        //            string qcScheme = parts[1].Trim();
        //            ddiQuestionLinkingFile lf = new ddiQuestionLinkingFile(questionLinkingFile, qcScheme);
        //            ddiQuestionLinkingFileList.Add(lf);
        //        }

        //    }
        //}
 
        //public void AddDerivationList(string importDerivationPath)
        //{
        //    ddiDerivationFileList = new List<ddiDerivationFile>();
        //    string[] lines = File.ReadAllLines(importDerivationPath);
        //    foreach (string line in lines)
        //    {
        //        string line2 = line.Trim();
        //        if (String.Equals(line2, ""))
        //        {
        //            continue;
        //        }
        //        else if (line2[0] == '#')
        //        {
        //            continue;
        //        }
        //        else
        //        {
        //            string[] parts = line2.Split(new char[] { '\t' });
        //            if (parts.Length != 2)
        //            {
        //                Console.WriteLine("Invalid line: " + line2);
        //                continue;
        //            }

        //            string derivationFile = parts[0].Trim();
        //            string vScheme = parts[1].Trim();
        //            ddiDerivationFile df = new ddiDerivationFile(derivationFile, vScheme);
        //            ddiDerivationFileList.Add(df);
        //        }

        //    }

        //}

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
