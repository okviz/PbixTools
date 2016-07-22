using CommandLine;
using CommandLine.Text;
using System.Diagnostics;
using System;


namespace PbixTools.CLI {

    class Options {
        [Option('r', "read", Required = true, HelpText = "Input file to be p rocessed.")]
        public string InputFile { get; set; }

        [Option('w', "write", Required = true, HelpText = "Output file to be created.")]
        public string OutputFile { get; set; }

        [Option('v', "verbose", DefaultValue = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [Option( "remove", DefaultValue = true, HelpText = "Remove unused visuals.")]
        public bool Remove { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage() {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        } 
    }

    class Program {

        static void Main(string[] args) {
            var options = new Options();

            if (CommandLine.Parser.Default.ParseArguments(args, options)) {
                // Set trace level
                TraceSwitch traceSwitch = new TraceSwitch("General", "Entire Application");
                traceSwitch.Level = (options.Verbose ? TraceLevel.Verbose : TraceLevel.Info);
                Trace.Listeners.Add(new ConsoleTraceListener());

                Globbing files = new Globbing(traceSwitch);
                PbixUtils utils = new PbixUtils(traceSwitch);
                foreach (var file in files.Glob(options.InputFile, options.OutputFile))
                {
                    Console.WriteLine(file.Item1, file.Item2);
                    if (options.Remove)
                       utils.RemoveUnusedVisuals(file.Item1, file.Item2);
                }
            }
        }
    }
}
