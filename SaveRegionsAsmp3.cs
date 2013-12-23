/* =======================================================================================================
 *	Script Name: Save Regions as mp3
 *	Description: This script iterates through regions in the open file, renders the region to an mp3,  
 *	                and saves the rendered file to a specified location.  
 *
 *      Title Metadata is specified based on the region name.  Track metadata is specified automatically
 *
 *      I'm pretty sure I borrowed the code here heavily from scripts that came w/ Sound Forge; and I have
 *      no idea what the license of said scripts are
 *
 *	Initial State: Run with a file open that contains regions.
 *
 *	Parameters (Args):
 *		type - the extension of the format to render regions to - DEFAULT: .pca
 *		preset - the name of the template to use for rendering - DEFAULT: prompt user
 *		dir - the directory the files should be rendered to - DEFAULT: c:\media\rips
 *
 *	Output: The queueing up of the file and the path name of the file
 *
 * ==================================================================================================== */

using System;
using System.IO;
using System.Windows.Forms;
using SoundForge;

//Run with a file that contains regions
//Iterates through the regions, renders to MP3 and saves the rendered file to c:\media\rip
//Scan the file for MODIFY HERE to see how to quickly customize for your own use

public class EntryPoint {
public string Begin(IScriptableApp app) {

   //start MODIFY HERE-----------------------------------------------
   string szType  = GETARG("type", ".mp3"); //choose any valid extension: .avi  .wav  .w64 .mpg .mp3 .wma .mov .rm .aif .ogg .raw .au .dig .ivc .vox .pca
   object vPreset = GETARG("preset", ""); //put the name of the template between the quotes, or leave blank to pop the Template chooser.
   string szDir   = GETARG("dir", ""); //hardcode a target path here

   // GETARG is a function that defines the default script settings. You can use the Script Args field to over-ride
   // the values within GETARG().
   // Example: To over-ride GETARG(Key, valueA), type Key=valueB in the Script Args field.
   //          Use an ampersand (&) to separate different Script Args: KeyOne=valueB&KeyTwo=valueC

   //Example Script Args: type=.wav&dir=f:\RegionFiles

   //end MODIFY HERE -----------------------------------

   ISfFileHost file = app.CurrentFile;
   if (null == file)
      return "Open a file containing regions before running this script. Script stopped.";
   if (null == file.Markers || file.Markers.Count <= 0)
      return "The file does not have any markers.";

   bool showMsg = true;
   if (szDir == null || szDir.Length <= 0)
   {
      szDir = SfHelpers.ChooseDirectory("Select the target folder for saved files:", @"C:\");
      showMsg = false;
   }
   if (szDir == null || szDir.Length <= 0)
      return "no output directory";

   //make sure the directory exists
   if(!Path.IsPathRooted(szDir))
   {
      string szBase2 = Path.GetDirectoryName(file.Filename);
      szDir = Path.Combine(szBase2, szDir);
   }

   Directory.CreateDirectory(szDir);

   ISfRenderer rend = null;
   if (szType.StartsWith("."))
       rend = app.FindRenderer(null, szType);
   else
       rend = app.FindRenderer(szType, null);

   if (null == rend)
      return String.Format("renderer for {0} not found.", szType);


   // if the preset parses as a valid integer, then use it as such, otherwise assume it's a string.
   try {
       int iPreset = int.Parse((string)vPreset);
       vPreset = iPreset;
   } catch (FormatException) {}

   ISfGenericPreset template = null;
   if ((string)vPreset != "")
      template = rend.GetTemplate(vPreset);
   else
      template = rend.ChooseTemplate((IntPtr)null, vPreset);
   if (null == template)
      return "Template not found. Script stopped.";

   string szBase = file.Window.Title;
   //check to see if the window title contains a file extension. If so, remove it for save name. Remove this if you want that extension.
   if (szBase.Contains("."))
   {
       int indexOf = szBase.LastIndexOf('.');
       szBase = szBase.Remove(indexOf, (szBase.Length - indexOf));
   }

   // JH adding this counter in order to keep track of count for naming files which is not always consecutive when having markers and regions in file
   int counter = 1;

   // calculate the number of regions
   // wish I could do this without two loops
   int numberOfRegions = 0;
   foreach (SfAudioMarker mk in file.Markers)
   {
      if (mk.Length <= 0)
         continue;
      numberOfRegions++;
   }

   // loop through all markers and generate files from regions
   foreach (SfAudioMarker mk in file.Markers)
   {
      if (mk.Length <= 0)
         continue;

      // set filename to TwoDigitTrackNumber-originalFileTitle-RegionName.mp3
      string szName = String.Format("{0}-{1}-{2}.{3}", counter.ToString("00"), szBase, mk.Name, rend.Extension);

      szName = SfHelpers.CleanForFilename(szName);
      DPF("Queueing: '{0}'", szName);

      string szFullName = Path.Combine(szDir, szName);
      if (File.Exists(szFullName))
          File.Delete(szFullName);

      SfAudioSelection  range = new SfAudioSelection(mk.Start, mk.Length);

      // set the name and track metadata for the output mp3
      // all other mp3 metadata will come from the template; which the user selected
      file.Summary.Title = mk.Name;
      file.Summary.TrackNo = String.Format("{0}/{1}",counter.ToString(), numberOfRegions.ToString());

      file.RenderAs(szFullName, rend.Guid, template, range, RenderOptions.RenderOnly);
      DPF("Path: '{0}'", szFullName);

      // increment the track number counter
      counter++;
   }

   // remove the summary info we set on the file
   file.Summary.Title = "";
   file.Summary.TrackNo = "";

   if(showMsg)
      MessageBox.Show(String.Format("Files are saving to: {0}", szDir), "Status", MessageBoxButtons.OK, MessageBoxIcon.Information);

   return null;
}

public void FromSoundForge(IScriptableApp app) {
   ForgeApp = app; //execution begins here
   app.SetStatusText(String.Format("Script '{0}' is running.", Script.Name));
   string msg = Begin(app);
   app.SetStatusText((msg != null) ? msg : String.Format("Script '{0}' is done.", Script.Name));
}
public static IScriptableApp ForgeApp = null;
public static void DPF(string sz) { ForgeApp.OutputText(sz); }
public static void DPF(string fmt, object o) { ForgeApp.OutputText(String.Format(fmt,o)); }
public static void DPF(string fmt, object o, object o2) { ForgeApp.OutputText(String.Format(fmt,o,o2)); }
public static void DPF(string fmt, object o, object o2, object o3) { ForgeApp.OutputText(String.Format(fmt,o,o2,o3)); }
public static string GETARG(string k, string d) { string val = Script.Args.ValueOf(k); if (val == null || val.Length == 0) val = d; return val; }
public static int    GETARG(string k, int d) { string s = Script.Args.ValueOf(k); if (s == null || s.Length == 0) return d; else return Script.Args.AsInt(k); }
public static bool   GETARG(string k, bool d) { string s = Script.Args.ValueOf(k); if (s == null || s.Length == 0) return d; else return Script.Args.AsBool(k); }
} //EntryPoint
