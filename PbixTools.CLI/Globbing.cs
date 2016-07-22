using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PbixTools.CLI
{
    public class Globbing
    {
        public enum Overwrite { YES, NO, ALL }

        private TraceSwitch traceSwitch;
        public Globbing(TraceSwitch traceSwitch)
        {
            this.traceSwitch = traceSwitch;
        }

        private Overwrite getFileOverwriteConfirmation(string filename)
        {
            do
            {
                Console.Write(String.Format("Overwrite file {0}? (Yes/No/All)[N]: ",Path.GetFileName(filename)));
                string response = Console.ReadLine();
                if (response.ToLower().Equals("y") || response.ToLower().Equals("yes"))
                {
                    return Overwrite.YES;
                }
                else if (response.ToLower().Equals("n") || response.ToLower().Equals("no") || string.IsNullOrEmpty(response))
                {
                    return Overwrite.NO;
                }
                else if (response.ToLower().Equals("a") || response.ToLower().Equals("all"))
                {
                    return Overwrite.ALL;
                }
            } while (true);
        }

        private string resolvePath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                string sourcedir = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(sourcedir))
                    sourcedir = Directory.GetCurrentDirectory();
                return Path.Combine(Path.GetFullPath(sourcedir), Path.GetFileName(path));
            } else
            {
                return path;
            }
        }

        public IEnumerable<Tuple<string,string>> Glob(string sourcepath, string destinationpath)
        {
            sourcepath = resolvePath(sourcepath);
            destinationpath = resolvePath(destinationpath);

            if (sourcepath.Contains("?") || sourcepath.Contains("*"))
            {
                if (Directory.Exists(destinationpath))
                {
                    if (Path.GetFullPath(Path.GetDirectoryName(sourcepath)).Equals(Path.GetFullPath(destinationpath)))
                    {
                        Trace.WriteLine("Error: source and destination directory need to be different");
                    } 
                    else 
                    {
                        bool overwriteAll = false;
                        foreach (string p in Directory.GetFiles(Path.GetDirectoryName(sourcepath), Path.GetFileName(sourcepath)))
                        {
                            string df = Path.Combine(destinationpath, Path.GetFileName(p));
                            if (File.Exists(df) && !overwriteAll)
                            {
                                switch (getFileOverwriteConfirmation(df))
                                {
                                    case Overwrite.ALL:
                                        overwriteAll = true;
                                        yield return Tuple.Create(p,df);
                                        break;
                                    case Overwrite.YES:
                                        yield return Tuple.Create(p, df);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                yield return Tuple.Create(p, df);
                            }
                        }
                    }
                  
                } else
                {
                    Trace.WriteLine(string.Format("Error: option w, when using wildcards {0} needs to be a directory and to exist", destinationpath));
                }
            }  else
            {
                if (File.Exists(sourcepath))
                {
                    yield return new Tuple<string, string>(sourcepath, destinationpath);
                } else
                {
                    Trace.WriteLine(string.Format("Error: option r, {0} does not exist", sourcepath));
                }
            }
        }

    }
}
