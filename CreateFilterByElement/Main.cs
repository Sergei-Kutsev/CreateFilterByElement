using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateFilterByElement
{
    private static ElementFilter CreateElementFilterFromFilterRules(IList<FilterRule> filterRules)
    {
        // We use a LogicalAndFilter containing one ElementParameterFilter
        // for each FilterRule. We could alternatively create a single
        // ElementParameterFilter containing the entire list of FilterRules.
        IList<ElementFilter> elemFilters = new List<ElementFilter>();
        foreach (FilterRule filterRule in filterRules)
        {
            ElementParameterFilter elemParamFilter = new ElementParameterFilter(filterRule);
            elemFilters.Add(elemParamFilter);
        }
        LogicalAndFilter elemFilter = new LogicalAndFilter(elemFilters);

        return elemFilter;
    }

    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIdoc = commandData.Application.ActiveUIDocument;
            Document doc = uIdoc.Document;

            Reference reference = uIdoc.Selection.PickObject(ObjectType.Element, new PickRebar(), "Выберите арматурный стержень на основе которого будет создаваться фильтр");
            Element element = doc.GetElement(reference);
            var rebar = element as Rebar;

            string selectedRebarComments = rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
            string selectedRebarDiameter = rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsValueString();



            View view = doc.ActiveView;

            // Mark categories to the new view filter
            List<ElementId> categories = new List<ElementId>();
            categories.Add(new ElementId(BuiltInCategory.OST_Rebar));
            //categories.Add(new ElementId(BuiltInCategory.OST_AreaRein));
           // categories.Add(new ElementId(BuiltInCategory.OST_PathRein));
            List<FilterRule> filterRules = new List<FilterRule>();

            

            using (Transaction t = new Transaction(doc, "Add view filter"))
            {
                t.Start();

                // Create filter element assocated to the input categories
                ParameterFilterElement parameterFilterElement = ParameterFilterElement.Create(doc, "Арматура Верхняя диаметром 16", categories);

                // Criterion 1 - wrebar comments
                ElementId rebarComments = new ElementId(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                filterRules.Add(ParameterFilterRuleFactory.CreateContainsRule(rebarComments, selectedRebarComments, true));

                // Criterion 2 - rebar diameter
                ElementId diameterRebar = new ElementId(BuiltInParameter.REBAR_BAR_DIAMETER);

                filterRules.Add(ParameterFilterRuleFactory.CreateEqualsRule(diameterRebar, selectedRebarDiameter, true));

                view.AddFilter(parameterFilterElement.Id);
                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();

                //LinePattern linePattern = new LinePattern();
                //FilteredElementCollector lineType = new FilteredElementCollector(doc);



                overrideGraphicSettings.SetProjectionLineColor(new Color(255, 0, 0));
                overrideGraphicSettings.SetProjectionLineWeight(5);
              //  overrideGraphicSettings.SetProjectionLinePatternId(linePattern.Id);

                

             

                view.SetFilterOverrides(parameterFilterElement.Id, overrideGraphicSettings); //настроили цвет и толщину линий
               // view.SetFilterVisibility(parameterFilterElement.Id, false);
                

                t.Commit();
            }
            return Result.Succeeded;
        }
    }
}

public class PickRebar : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        return elem.Category != null && elem is Rebar;
    }
    public bool AllowReference(Reference reference, XYZ position)
    {
        throw new NotImplementedException();
    }
}
