using System;
using System.IO;
using System.Security.Permissions;

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class Startup
{
	private static string importDir;

#pragma warning disable 0414
	private static StreamWriter lockFile;
#pragma warning restore 0414

	private static string dataPath;

	static Startup()
	{
		InitializeFileWatcher();
		Debug.Log("File system watcher initialized");
	}

	[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
	private static void InitializeFileWatcher()
	{
		dataPath = Application.dataPath;
		importDir = Path.Combine(Path.Combine(Path.GetTempPath(), "Unity3DPlugin"), GetProjectName());
		Directory.CreateDirectory(importDir);
		lockFile = File.AppendText(Path.Combine(importDir, "lock"));

		// Create a new FileSystemWatcher and set its properties.
		FileSystemWatcher watcher = new FileSystemWatcher();
		watcher.Path = importDir;
		/* Watch for changes in LastAccess and LastWrite times, and
           the renaming of files or directories. */
		watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
			| NotifyFilters.FileName | NotifyFilters.DirectoryName;
		// Only watch import files.
		watcher.Filter = "*.x3d";

		// Add event handlers.
		watcher.Created += new FileSystemEventHandler(OnCreate);

		// Begin watching.
		watcher.EnableRaisingEvents = true;
	}

	private static string GetProjectName()
	{
		string[] pathElements = dataPath.Split("/"[0]);
		return pathElements[pathElements.Length - 2];
	}

	// TODO: Error-Handling, Auto-Focus Unity window etc.
	private static void OnCreate(object source, FileSystemEventArgs e)
	{

		Debug.Log("Importing: " + e.FullPath);

		/*System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
		startInfo.FileName = "C:/Program Files/Blender Foundation/Blender/blender.exe";
		startInfo.Arguments = string.Format("--background --python \"{0}\" -- \"{1}\"", dataPath + "/ParaView/Editor/blender_post_process.py", e.FullPath);
		startInfo.RedirectStandardOutput = true;
		startInfo.RedirectStandardError = true;
		startInfo.UseShellExecute = false;
		startInfo.CreateNoWindow = true;

		System.Diagnostics.Process p = new System.Diagnostics.Process();
		p.StartInfo = startInfo;
		p.EnableRaisingEvents = true;
		try
		{
			p.Start();
			p.WaitForExit();
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message);
			//throw ex;
		}

		string line = p.StandardOutput.ReadLine();
		while(line != null) {
			Debug.Log(line);
			line = p.StandardOutput.ReadLine();
		}
		string blenderFile = e.FullPath.Replace(".x3d", ".blend");

		File.Delete(e.FullPath);
		File.Move(blenderFile, Path.Combine(Path.Combine(dataPath, "ParaView"), Path.GetFileName(blenderFile)));*/
	}
}
