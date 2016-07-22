using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.IO.Packaging;

namespace PbixTools.CLI {

    public class PbixUtils {
        private TraceSwitch traceSwitch;
        public PbixUtils(TraceSwitch traceSwitch) {
            this.traceSwitch = traceSwitch;
        }
        public void RemoveUnusedVisuals(string inputFile, string outputFile) {
            try {
                File.Copy(inputFile, outputFile, true);
                using (FileStream zipToOpen = new FileStream(outputFile, FileMode.Open))
                {
                    using (Package archive = Package.Open(zipToOpen,FileMode.Open,FileAccess.ReadWrite))
                    {
                        List<string> resourcePackageDirs = GetPackages(archive);
                        JObject o = ProcessLayout(archive, resourcePackageDirs);
                        if (o != null)
                            ReplaceLayout(archive, o);
                    }
                }
            }
            catch (System.IO.FileNotFoundException) {
                Trace.WriteLine(string.Format("Error: The source file is not found."));
            }
            catch (System.IO.InvalidDataException) {
                Trace.WriteLine(string.Format("Error: The source file is not a valid pbix file."));
            }
        }

        private void ReplaceLayout(Package archive, JObject o) {
            Uri partLayout = PackUriHelper.CreatePartUri(new Uri(@"Report\Layout", UriKind.Relative));
            archive.DeletePart(partLayout);

            string tempfile = Path.GetTempFileName();
            using (StreamWriter writer = new StreamWriter(File.Create(tempfile), new UnicodeEncoding(false, false))) {
                using (JsonWriter jw = new JsonTextWriter(writer)) {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(jw, o);
                }
            }

            PackagePart packagePartLayout = archive.CreatePart(partLayout, "application/json",CompressionOption.Normal);
            using (FileStream fileStream = new FileStream(tempfile, FileMode.Open, FileAccess.Read))
            {
                CopyStream(fileStream, packagePartLayout.GetStream());
            }
           
            File.Delete(tempfile);
        }

        private static void CopyStream(Stream source, Stream target)
        {
            const int bufSize = 0x1000;
            byte[] buf = new byte[bufSize];
            int bytesRead = 0;
            while ((bytesRead = source.Read(buf, 0, bufSize)) > 0)
                target.Write(buf, 0, bytesRead);
        }

        private JObject ProcessLayout(Package archive, List<string> resourcePackageDirs) {
            JObject o = null;
            try
            {
                Uri partLayout = PackUriHelper.CreatePartUri(new Uri(@"Report\Layout", UriKind.Relative));
                PackagePart layoutPart = archive.GetPart(partLayout);
                using (StreamReader reader = new StreamReader(layoutPart.GetStream(), Encoding.Unicode))
                {
                    string str = reader.ReadToEnd();
                    o = JObject.Parse(str);
                    string strwithoutresourcepackages = str;
                    JObject owithout = JObject.Parse(strwithoutresourcepackages);
                    owithout.Remove("resourcePackages");
                    strwithoutresourcepackages = JsonConvert.SerializeObject(owithout);
                    FindUnreferencedPackages(resourcePackageDirs, o, strwithoutresourcepackages);
                    RemovePackages(archive, resourcePackageDirs);
                }
            } catch ( Exception  ){ }
            return o;
        }

        private void FindUnreferencedPackages(List<string> resourcePackageDirs, JObject o, string strwithoutresourcepackages) {
            List<JToken> tokenToDelete = new List<JToken>();
            if (o["resourcePackages"] != null)
            {
                foreach (var item in o["resourcePackages"])
                {
                    string name = item["resourcePackage"]["name"].ToString();
                    int c = Regex.Matches(strwithoutresourcepackages, name).Count;
                    if (traceSwitch.TraceVerbose)
                    {
                        Trace.WriteLine(string.Format("Layout contains package {0}, referenced {1} times", name, c));
                    }
                    if (c == 0)
                    {
                        if (traceSwitch.TraceVerbose)
                        {
                            Trace.WriteLine(string.Format("Deleting package node {0}", name));
                        }
                        tokenToDelete.Add(item);
                    }
                    else
                    {
                        resourcePackageDirs.Remove(name);
                    }
                }

                foreach (JToken t in tokenToDelete)
                {
                    t.Remove();
                }
            }
        }

        private void RemovePackages(Package archive, List<string> resourcePackageDirs) {
            foreach (string notfound in resourcePackageDirs) {
                if (traceSwitch.TraceVerbose) {
                    Trace.WriteLine(string.Format("Removing package {0}", notfound));
                }

                List<Uri> toDelete = new List<Uri>();
                foreach (PackagePart pp in archive.GetParts())
                    if (pp.Uri.ToString().StartsWith("/Report/CustomVisuals/" + notfound + "/"))
                        toDelete.Add(pp.Uri);
                
                foreach (Uri de in toDelete) {
                    if (traceSwitch.TraceVerbose) {
                        Trace.WriteLine(string.Format("Deleting {0}", de));
                    }
                    archive.DeletePart(de);
                }
            }
        }


        private List<string> GetPackages(Package archive) {
            List<string> resourcePackageDirs = new List<string>();
            Regex regex = new Regex(@"Report/CustomVisuals/(.*?)/package.json");
            foreach (PackagePart e in archive.GetParts()) {
                Match match = regex.Match(e.Uri.ToString());
                if (match.Success) {
                    resourcePackageDirs.Add(match.Groups[1].Value);
                    if (traceSwitch.TraceVerbose) {
                        Trace.WriteLine(string.Format("Zip contains package {0}", match.Groups[1].Value));
                    }
                }
            }

            return resourcePackageDirs;
        }
    }
}
