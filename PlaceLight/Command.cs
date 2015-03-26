#region Namespaces
using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;
#endregion

namespace PlaceLight
{
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

  [Transaction( TransactionMode.Manual )]
  public class Command : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      var uiApp = commandData.Application;
      var doc = uiApp.ActiveUIDocument.Document;

      try
      {
        Selection selection = uiApp.ActiveUIDocument
          .Selection;

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

        // Determine the host BIM element.

        Element host = lightFamilyInstance.Host;

        // Get new light location.

        XYZ placeXyzPoint = selection.PickPoint(
          "Select Point to place light:" );

        // Assuming the ceiling is horizontal, set
        // the location point Z value for the copy
        // equal to the original.

        placeXyzPoint = new XYZ( placeXyzPoint.X,
          placeXyzPoint.Y, ( lightFamilyInstance
            .Location as LocationPoint ).Point.Z );

        // All lighting fixtures are non-strucutral.

        Autodesk.Revit.DB.Structure.StructuralType
          non_structural = Autodesk.Revit.DB.Structure
            .StructuralType.NonStructural;

        using( var trans = new Transaction( doc ) )
        {
          trans.Start( "LightArray" );

          // Start placing lights.

          FamilyInstance lightFamilyInstance2
            = doc.Create.NewFamilyInstance(
              placeXyzPoint, lightFamilySymbol,
              host, non_structural );

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
