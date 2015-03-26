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
    static FamilyInstance PlaceALight( 
      XYZ lightPlacePoint, 
      Element host,
      FamilySymbol lightSymbol )
    {
      Document doc = lightSymbol.Document;

#if CREATE_INSTANCE_ON_NEW_REFERENCE_PLANE
      // I tried every combination of numbers here and nothing worked.

      XYZ bubbleEnd = new XYZ( 5, 0, 0 );
      XYZ freeEnd = new XYZ( -5, 0, 0 );
      XYZ thirdPt = new XYZ( 0, 0, 1 );

      ReferencePlane referencePlane 
        = doc.Create.NewReferencePlane2( bubbleEnd, 
          freeEnd, thirdPt, doc.ActiveView );
      
      XYZ xAxisOfPlane = new XYZ( 0, 0, -1 );

      // Error here:

      doc.Create.NewFamilyInstance( 
        referencePlane.Reference, lightPlacePoint, 
        xAxisOfPlane, lightSymbol );
#endif // CREATE_INSTANCE_ON_NEW_REFERENCE_PLANE

      return doc.Create.NewFamilyInstance( 
        lightPlacePoint, lightSymbol, host, 
        Autodesk.Revit.DB.Structure.StructuralType
          .NonStructural );
    }

    public Result Execute( 
      ExternalCommandData commandData, 
      ref string message, 
      ElementSet elements )
    {
      var uiApp = commandData.Application;
      var doc = uiApp.ActiveUIDocument.Document;

      try
      {
        Selection selection = uiApp.ActiveUIDocument.Selection;

        // Pick a light fixture

        var pickedLightReference = selection.PickObject( 
          ObjectType.Element, new LightPickFilter(), 
          "Please select lighting fixture to place" );

        if( pickedLightReference == null )
        {
          return Result.Failed;
        }

        // Get Family Instance of the selected light reference

        FamilyInstance lightFamilyInstance 
          = doc.GetElement( pickedLightReference ) 
            as FamilyInstance;

        // Get FamilySymbol of the family instance

        if( lightFamilyInstance == null )
        {
          return Result.Failed;
        }

        FamilySymbol lightFamilySymbol 
          = lightFamilyInstance.Symbol;

        // Get new light location

        XYZ placeXyzPoint = selection.PickPoint( 
          "Select Point to place light:" );

        using( var trans = new Transaction( doc ) )
        {
          trans.Start( "LightArray" );

          // Start placing lights

          PlaceALight( placeXyzPoint, lightFamilyInstance.Host, lightFamilySymbol );

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
