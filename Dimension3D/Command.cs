using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace Dimension3D
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Autodesk.Revit.DB.Document revitDoc = commandData.Application.ActiveUIDocument.Document;  //取得文档           
            Application revitApp = commandData.Application.Application;             //取得应用程序            
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;           //取得当前活动文档        


            Selection sel = uiDoc.Selection;
            Reference ref1 = sel.PickObject(ObjectType.Element, "选择一个注释");
            Element elem = revitDoc.GetElement(ref1);

            Dimension dimension = elem as Dimension;
            Curve curve = dimension.Curve;
            Line line = curve as Line;
            //XYZ SPoint =  line.Origin; 这个注释的起点很神奇，不准而且还会变

            XYZ CPoint = dimension.Origin;
            double LineLength = Convert.ToDouble(dimension.Value / 2);

            XYZ vec = line.Direction;
            //计算出最长的线的起始点，并创建线
            XYZ EPoint = new XYZ(CPoint.X + LineLength * vec.X, CPoint.Y + LineLength * vec.Y, CPoint.Z + LineLength * vec.Z);
            XYZ SPoint = new XYZ(CPoint.X - LineLength * vec.X, CPoint.Y - LineLength * vec.Y, CPoint.Z - LineLength * vec.Z);
            Line line1 = Line.CreateBound(SPoint, EPoint);


            //XYZ temp = new XYZ(1, 1, 1);
            //XYZ normal = line.Direction.CrossProduct(temp);

            //应该去拿注释所在平面
            SketchPlane sketchPlane = dimension.View.SketchPlane;
            XYZ normal = sketchPlane.GetPlane().Normal;


            //创建注释标
            XYZ xYZ = normal.CrossProduct(line.Direction);
            double Dlength = 15;
            XYZ SPoint2 = new XYZ(SPoint.X + xYZ.X * LineLength / Dlength, SPoint.Y + xYZ.Y * LineLength / Dlength, SPoint.Z + xYZ.Z * LineLength / Dlength);
            XYZ SPoint3 = new XYZ(SPoint.X - xYZ.X * LineLength / Dlength, SPoint.Y - xYZ.Y * LineLength / Dlength, SPoint.Z - xYZ.Z * LineLength / Dlength);

            XYZ SPoint4 = new XYZ(EPoint.X + xYZ.X * LineLength / Dlength, EPoint.Y + xYZ.Y * LineLength / Dlength, EPoint.Z + xYZ.Z * LineLength / Dlength);
            XYZ SPoint5 = new XYZ(EPoint.X - xYZ.X * LineLength / Dlength, EPoint.Y - xYZ.Y * LineLength / Dlength, EPoint.Z - xYZ.Z * LineLength / Dlength);

            Line line2 = Line.CreateBound(SPoint2, SPoint3);
            Line line3 = Line.CreateBound(SPoint4, SPoint5);



            XYZ temp = new XYZ(1, 1, 1);
            XYZ normal2 = line2.Direction.CrossProduct(temp);
            XYZ normal3 = line3.Direction.CrossProduct(temp);

            //创建注释的起始终止线
            using (Transaction tran = new Transaction(uiDoc.Document))
            {
                tran.Start("测试");

                uiDoc.Document.Create.NewModelCurve(line1, sketchPlane);

                Plane plane = Plane.CreateByNormalAndOrigin(normal2, SPoint);
                SketchPlane sketchPlane2 = SketchPlane.Create(uiDoc.Document, plane);
                uiDoc.Document.Create.NewModelCurve(line2, sketchPlane2);

                Plane plane1 = Plane.CreateByNormalAndOrigin(normal3, EPoint);
                SketchPlane sketchPlane3 = SketchPlane.Create(uiDoc.Document, plane1);
                uiDoc.Document.Create.NewModelCurve(line3, sketchPlane3);


                //放样成为一个体
                //createExtrusionProfile(commandData, line1, 10);

                tran.Commit();
            }
            return Result.Succeeded;
        }


        //本来想在这里做一个放样的，但是放样用api做需要建立族文件，然后再在族文件中创建放样，再导入进来，比较麻烦暂时就不做了，详参：
        //https://blog.csdn.net/niaxiapia/article/details/80513595?ops_request_misc=%25257B%252522request%25255Fid%252522%25253A%252522161121330016780265434271%252522%25252C%252522scm%252522%25253A%25252220140713.130102334.pc%25255Fall.%252522%25257D&request_id=161121330016780265434271&biz_id=0&utm_medium=distribute.pc_search_result.none-task-blog-2~all~first_rank_v2~rank_v29-4-80513595.pc_search_result_cache&utm_term=revit%E4%BA%8C%E6%AC%A1%E5%BC%80%E5%8F%91%20%E6%94%BE%E6%A0%B7

        private void createExtrusionProfile(ExternalCommandData commandData, Line line, Double radius)
        {
            Autodesk.Revit.DB.Document revitDoc = commandData.Application.ActiveUIDocument.Document;  //取得文档           
            Application revitApp = commandData.Application.Application;             //取得应用程序            
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;           //取得当前活动文档        

            Autodesk.Revit.DB.Document document = uiDoc.Document;

            //获取一个轮廓，此处就用圆形轮廓
            Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
            SketchPlane sketchPlane = SketchPlane.Create(document, plane);
            Arc arc = Arc.Create(plane, radius, 0, Math.PI * 2);

            //为下面的放样准备参数
            CurveArray curveArray = new CurveArray();
            CurveArrArray curveArrArray = new CurveArrArray();
            curveArray.Append(arc);
            curveArrArray.Append(curveArray);
            //放样所用的直线
            ReferenceArray referenceArray = new ReferenceArray();
            referenceArray.Append(line.Reference);

            Autodesk.Revit.Creation.Application application = uiDoc.Application.Application.Create;
            SweepProfile sweepProfile = application.NewCurveLoopsProfile(curveArrArray);

            //这句话是不对的，原因是在项目文档中无法拉伸，应该在族文档中实现
            //Sweep sweep = document.FamilyCreate.NewSweep(true, referenceArray, sweepProfile, 0, ProfilePlaneLocation.Start);
        }
    }
}
