#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;
#endregion

namespace PlaceLight
{
  #region LightPickFilter
  public class LightPickFilter : ISelectionFilter
  {
    public bool AllowElement( Element e )
    {
      return e.Category.Id.IntegerValue.Equals(
        (int) BuiltInCategory.OST_LightingFixtures );
    }

    public bool AllowReference( Reference r, XYZ p )
    {
      return false;
    }
  }
  #endregion // LightPickFilter

  [Transaction( TransactionMode.Manual )]
  public class Command : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      try
      {
        Selection selection = uidoc.Selection;

        // Pick a light fixture.

        var pickedLightReference = selection.PickObject(
          ObjectType.Element, new LightPickFilter(),
          "Please select lighting fixture to place" );

        if( null == pickedLightReference )
        {
          return Result.Failed;
        }

        // Get Family Instance of the selected light reference.

        FamilyInstance lightFamilyInstance
          = doc.GetElement( pickedLightReference )
            as FamilyInstance;

        // Get FamilySymbol of the family instance.

        if( lightFamilyInstance == null )
        {
          return Result.Failed;
        }

        FamilySymbol lightFamilySymbol
          = lightFamilyInstance.Symbol;

        FamilyPlacementType placementType 
          = lightFamilySymbol.Family
            .FamilyPlacementType;

        // Determine the host BIM element.

        Element host = lightFamilyInstance.Host;

        // What else can we find out?

        LocationPoint lp = lightFamilyInstance.Location
          as LocationPoint;

        IList<FamilyPointPlacementReference> refs 
          = lightFamilyInstance
            .GetFamilyPointPlacementReferences();

        Reference hostFace = lightFamilyInstance.HostFace;
        ElementId levelId = lightFamilyInstance.LevelId;

        GeometryObject faceObj 
          = host.GetGeometryObjectFromReference( 
            hostFace );

        Face face = faceObj as Face;

        // Get new light location.

        XYZ placeXyzPoint = selection.PickPoint(
          "Select point to place new light:" );

        // Assuming the ceiling is horizontal, set
        // the location point Z value for the copy
        // equal to the original.

        placeXyzPoint = new XYZ( placeXyzPoint.X,
          placeXyzPoint.Y, lp.Point.Z );

        // All lighting fixtures are non-strucutral.

        Autodesk.Revit.DB.Structure.StructuralType
          non_structural = Autodesk.Revit.DB.Structure
            .StructuralType.NonStructural;

        using( var trans = new Transaction( doc ) )
        {
          trans.Start( "LightArray" );

          if( faceObj is PlanarFace )
          {
            PlanarFace pf = faceObj as PlanarFace;
            Plane plane = new Plane( pf.Normal, pf.Origin );
          }

          //SketchPlane sp = SketchPlane.Create( doc, hostFace );
          //uidoc.ActiveView.SketchPlane = sp;

          //doc.Regenerate();

          // Start placing lights.

          // This is not suitabel for work plane based symbols:
          //
          //FamilyInstance lightFamilyInstance2
          //  = doc.Create.NewFamilyInstance(
          //    placeXyzPoint, lightFamilySymbol,
          //    host, non_structural );

          // This throws: 
          // Family cannot be placed as line-based on an input face reference, 
          // because its FamilyPlacementType is not WorkPlaneBased or CurveBased
          // Parameter name: symbol
          //
          //Line line = Line.CreateBound( placeXyzPoint, 
          //  placeXyzPoint + XYZ.BasisX );
          //FamilyInstance lightFamilyInstance2
          //  = doc.Create.NewFamilyInstance(
          //    hostFace, line, lightFamilySymbol );

          // This throws:
          // The Reference of the input face is null.  
          // If the face was obtained from Element.Geometry, make sure to turn on the option 'ComputeReferences'.
          // Parameter name: face
          //
          //FamilyInstance lightFamilyInstance3
          //  = doc.Create.NewFamilyInstance(
          //    face, placeXyzPoint, XYZ.BasisX,
          //    lightFamilySymbol );

          FamilyInstance lightFamilyInstance3
            = doc.Create.NewFamilyInstance( 
              hostFace, placeXyzPoint, XYZ.BasisX, 
              lightFamilySymbol );

          trans.Commit();
        }
      }
      catch( OperationCanceledException )
      {
        return Result.Cancelled;
      }
      catch( Exception ex )
      {
        message = ex.Message;
        return Result.Failed;
      }
      return Result.Succeeded;
    }
  }
}
