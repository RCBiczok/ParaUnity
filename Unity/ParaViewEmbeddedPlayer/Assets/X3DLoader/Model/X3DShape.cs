namespace ParaUnity.X3D
{
	using UnityEngine;
	using System.Linq;
	using System.Collections.Generic;
	using System.Xml.Linq;

	public class X3DShape : X3DNode
	{
		public X3DAppearance Appearance { get; private set; }
		public X3DGeometry Geometry { get; private set; }

		public X3DShape (X3DAppearance appearance, X3DGeometry geometry)
		{
			this.Appearance = appearance;
			this.Geometry = geometry;
		}


		override public void Convert (GameObject parent)
		{
			GameObject shapeObj = new GameObject (this.GetType().Name);
			shapeObj.transform.parent = parent.transform;

			if (this.Geometry != null) {
				this.Geometry.Convert (shapeObj);
			}
			//this.Appearance.Convert (shapeObj);
		}

	}

	sealed class X3DShapeHandler : X3DHandler
	{

		private X3DAppearanceHandler appearanceHandler = new X3DAppearanceHandler();

		private X3DGeometryHandler[] geometryHandlers = new X3DGeometryHandler[] { 
			new X3DIndexedFaceSetHandler (),
			new X3DIndexedLineSetHandler (),
			new X3DPointSetHandler()
		};

		public X3DShapeHandler() : base("Shape") {
		}

		public override X3DNode Parse (XElement elem)
		{
			X3DAppearance appearance = (X3DAppearance)appearanceHandler.Parse (elem.Element ("Appearance"));
			X3DGeometry geometry = ParseGeometryNode (elem);
			return new X3DShape(appearance, geometry);
		}
			
		public X3DGeometry ParseGeometryNode (XElement elem)
		{
			Dictionary<string, X3DGeometryHandler> handlerDict = geometryHandlers.ToDictionary (h => h.TargetNodeName, h => h);

			X3DGeometry[] geoElements = elem.Elements ().
				Where (e => handlerDict.ContainsKey (e.Name.ToString ())).
				Select (e => handlerDict [e.Name.ToString ()].ParseGeometry (e)).ToArray ();

			return geoElements.Length > 0  ? geoElements[0] : null;
		}

	}
}

