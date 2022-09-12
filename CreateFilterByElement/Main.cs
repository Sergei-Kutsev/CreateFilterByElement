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
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        //создаем метод, который принимает список с правилами фильтра
        static public ElementFilter CreateElementFilterFromFilterRules(List<FilterRule> filterRules)
        {
            //создаем список с элементами фильтра
            List<ElementFilter> elemFilters = new List<ElementFilter>();

            //перебираем правила фильтра и для каждого правила 
            foreach (FilterRule filterRule in filterRules)
            {
                // создаем фильтр, используемый для сопоставления элементов по одному или нескольким правилам фильтрации параметров.
                ElementParameterFilter elemParamFilter = new ElementParameterFilter(filterRule);

                elemFilters.Add(elemParamFilter);
            }
            LogicalAndFilter elemFilter = new LogicalAndFilter(elemFilters);
            return elemFilter;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uIdoc = commandData.Application.ActiveUIDocument;
            Document doc = uIdoc.Document;

            View view = doc.ActiveView;


            Reference reference = uIdoc.Selection.PickObject(ObjectType.Element, new PickRebar(), "Выберите арматурный стержень на основе которого будет создаваться фильтр");
            Element element = doc.GetElement(reference);
            var rebar = element as Rebar;

            string selectedRebarComments = rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
            double selectedRebarDiameter = rebar.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER).AsDouble();


            using (Transaction t = new Transaction(doc, "Add view filter"))
            {
                t.Start();

                //создаем список категорий фильтра
                List<ElementId> categories = new List<ElementId>();
                //добавляем в список одну категорию - арматурные стержни
                categories.Add(new ElementId(BuiltInCategory.OST_Rebar));

                //создаем пустой список с правилами для будущего фильтра
                List<FilterRule> filterRules = new List<FilterRule>();

                // создаем фильтр в документе с соответсвующими категориями, в нашем случае это только арматурные стержни
                ParameterFilterElement parameterFilterElement = ParameterFilterElement.Create(doc, "Арматура ф16 Верхняя", categories);

                // объявляем переменную с параметром "комментарий"
                ElementId rebarComments = new ElementId(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                // добавляем в созданный список с правилами переменную с параметром "функция"
                filterRules.Add(ParameterFilterRuleFactory.CreateEqualsRule(rebarComments, selectedRebarComments, true));

                // объявляем переменную с параметром "диаметр стержня"
                ElementId rebarDiameter = new ElementId(BuiltInParameter.REBAR_BAR_DIAMETER);
                // добавляем в созданный список с правилами переменную с параметром "длина стены"
                filterRules.Add(ParameterFilterRuleFactory.CreateGreaterOrEqualRule(rebarDiameter, selectedRebarDiameter, 0.00000000000000000001));

                // создаем переменную на которой вызываем метод и передаем в метод список с двумя созданными правилами
                ElementFilter elemFilter = CreateElementFilterFromFilterRules(filterRules);
                parameterFilterElement.SetElementFilter(elemFilter);



                view.AddFilter(parameterFilterElement.Id);
                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();

                //LinePattern linePattern = new LinePattern();
                //FilteredElementCollector lineType = new FilteredElementCollector(doc);


                overrideGraphicSettings.SetCutLineColor(new Color(255, 0, 0));
                overrideGraphicSettings.SetCutLineWeight(5);
                overrideGraphicSettings.SetProjectionLineColor(new Color(255, 0, 0));
                overrideGraphicSettings.SetProjectionLineWeight(5);

                //  overrideGraphicSettings.SetProjectionLinePatternId(linePattern.Id);

                view.SetFilterOverrides(parameterFilterElement.Id, overrideGraphicSettings); //настроили цвет и толщину линий

                t.Commit();
            }
            return Result.Succeeded;
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
}
