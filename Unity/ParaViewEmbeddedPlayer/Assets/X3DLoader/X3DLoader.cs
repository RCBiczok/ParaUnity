namespace ParaUnity.X3D
{
	using System;
	using System.Globalization;
	using System.Collections.Generic;
	using System.Linq;
	using System.Xml.Linq;
	using UnityEngine;

	public class X3DLoader
	{

		public List<X3DMesh> Load (string x3dFile)
		{
			XDocument doc = XDocument.Load (x3dFile);
			XNamespace df = doc.Root.Name.Namespace;
			var results = from request in doc.Descendants (df + "IndexedFaceSet")
				select new IndexedFaceSetHandler().Parse(df, request);
			return results.ToList ();
		}
	}
}