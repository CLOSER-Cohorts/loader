using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CloserDataPipeline.Steps
{
    public class MapVariablesToQuestions : IPipelineStep
    {
        string fileName;
        string ccsName;
        List<string> ccsNameList;
        string vsName;

        public List<IVersionable> WorkingSet { get; set; }

        public string Name
        {
            get { return "Map Variables to Questions - " + Path.GetFileName(fileName); }
        }

        public MapVariablesToQuestions(string fileName, string ccsName, List<string> ccsNameList, string vsName)
        {
            this.fileName = fileName;
            this.ccsName = ccsName;
            this.ccsNameList = ccsNameList;
            this.vsName = vsName;
        }

        public void Execute()
        {
            // The file format is:
            //   [QuestionName] [Tab] [VariableName]
            // or in the case of multiple source instruments:
            //   [QuestionConstructSchemeName] [tab] [QuestionName] [Tab] [VariableName]
            // If [QuestionName] or [VariableName] is "0", this is treated as no mapping.
            //

            if (!System.IO.File.Exists(fileName))
            {
                Trace.WriteLine("   missing file: " + fileName);
                return;
            }
            
            // Read each line.
            string[] lines = File.ReadAllLines(this.fileName);
            foreach (string line in lines)
            {
                // Break the line apart by the tab character.
                // If there are two parts:
                // The question construct scheme is the one of the config file.
                // The left side is the question name. 
                // The right side is the variable name.
                // If there are three parts, check that the qc scheme is in the configuration, shift the parts
                string questionConstructScheme;
                string questionName;
                string variableName;


                string[] parts = line.Split(new char[] { '\t' });

                if (parts.Length == 2)
                {
                    questionConstructScheme = this.ccsName;
                    string questionColumn = parts[0].Trim();
                    string[] questionNameParts = questionColumn.Split(new char[] { '$' });     //remove grid cell info
                    questionName = questionNameParts[0];
                    variableName = parts[1].Trim();
                }
                else if (parts.Length == 3)
                {
                    questionConstructScheme = parts[0].Trim();
                    if (this.ccsNameList.Contains(questionConstructScheme) == false)
                    {
                        Trace.WriteLine("      invalid question scheme: " + line);
                        continue;
                    }
                    string questionColumn = parts[1].Trim();
                    string[] questionNameParts = questionColumn.Split(new char[] { '$' });     //remove grid cell info
                    questionName = questionNameParts[0];
                    variableName = parts[2].Trim();
                }
                else
                {
                    Trace.WriteLine("      invalid line: " + line);
                    continue;
                }

                // Skip lines with the question name set to "0".
                if (questionName == "0")
                {
                    continue;
                }
                // Skip lines with the variable name set to "0".
                if (variableName == "0")
                {
                    continue;
                }

                //Trace.WriteLine("   Working with [" + ccsName + ", " + vsName + "] " + questionName + " and " + variableName);

                // The mapping file points to the name of the QuestionConstruct, not the QuestionItem.
                // Look this up in the right control construct scheme and then look up the question or question grid from that.
                // Look up the question in the working set.
                var matchingQuestionConstructs = WorkingSet.OfType<ControlConstructScheme>().
                        Where(x => string.Compare(x.ItemName.Best, questionConstructScheme, ignoreCase: true) == 0).First().
                        ControlConstructs.OfType<QuestionActivity>().
                        Where(x => string.Compare(x.ItemName.Best, questionName, ignoreCase: true) == 0);
                        
                if (matchingQuestionConstructs.Count() == 0)
                {
                    Trace.WriteLine("      no question named " + questionName);
                    continue;
                }
                else if (matchingQuestionConstructs.Count() > 1)
                {
                    Trace.WriteLine("      multiple questions named " + questionName);
                    continue;
                }

                var questionConstruct = matchingQuestionConstructs.First();

                //follow through to question item or grid
                if ((questionConstruct.Question == null) && (questionConstruct.QuestionGrid == null))
                {
                    Trace.WriteLine("      question construct does not have question or question grid: " + questionName);
                    continue;
                }

                //question items
                else if (questionConstruct.Question != null)
                {
                    var question = questionConstruct.Question;

                    // Look up the variable in the relevant variable scheme in the working set.
                    //var matchingVariables = WorkingSet.OfType<Variable>()
                    var matchingVariables = WorkingSet.OfType<VariableScheme>().
                        Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase:true) == 0).First().Variables.
                        Where(x => string.Compare(x.ItemName.Best, variableName, ignoreCase:true) == 0);

                    if (matchingVariables.Count() == 0)
                    {
                        Trace.WriteLine("        [item] no variable named " + variableName);
                        continue;
                    }
                    else if (matchingVariables.Count() > 1)
                    {
                        Trace.WriteLine("        [item] multiple variables named " + variableName);
                        continue;
                    }

                    var variable = matchingVariables.First();

                    // If we have correct matches, add the question as a source to the variable.
                    variable.SourceQuestions.Add(question);
                    //variable.SourceParameter.
                }

                //question grids
                else
                {
                    var questionGrid = questionConstruct.QuestionGrid;

                    // Look up the variable  in the relevant variable scheme in the working set.
                    //var matchingVariables = WorkingSet.OfType<Variable>()
                    //.Where(x => string.Compare(x.ItemName.Best, variableName, ignoreCase:true) == 0);
                    var matchingVariables = WorkingSet.OfType<VariableScheme>().
                        Where(x => string.Compare(x.ItemName.Best, this.vsName, ignoreCase: true) == 0).First().Variables.
                        Where(x => string.Compare(x.ItemName.Best, variableName, ignoreCase: true) == 0);
                    if (matchingVariables.Count() == 0)
                    {
                        Trace.WriteLine("        [grid] no variable named " + variableName);
                        continue;
                    }

                    foreach (var variable in matchingVariables)
                    {
                        //variable.SourceQuestions.Add(questionGrid)
                    }
                }

            }
        }
    }
}
